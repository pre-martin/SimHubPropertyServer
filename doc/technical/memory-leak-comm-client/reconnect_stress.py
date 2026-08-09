#!/usr/bin/env python3
import argparse
import random
import socket
import struct
import threading
import time


class Stats:
    def __init__(self):
        self.lock = threading.Lock()
        self.connect_ok = 0
        self.connect_fail = 0
        self.sessions = 0
        self.send_fail = 0
        self.recv_lines = 0
        self.worker_errors = 0

    def inc(self, name, n=1):
        with self.lock:
            setattr(self, name, getattr(self, name) + n)

    def snapshot(self):
        with self.lock:
            return {
                "connect_ok": self.connect_ok,
                "connect_fail": self.connect_fail,
                "sessions": self.sessions,
                "send_fail": self.send_fail,
                "recv_lines": self.recv_lines,
                "worker_errors": self.worker_errors,
            }


def send_line(sock, line, lock, stats):
    data = (line + "\r\n").encode("utf-8", errors="replace")
    try:
        with lock:
            sock.sendall(data)
        return True
    except Exception:
        stats.inc("send_fail")
        return False


def abrupt_close(sock):
    try:
        linger = struct.pack("hh", 1, 0)
        sock.setsockopt(socket.SOL_SOCKET, socket.SO_LINGER, linger)
    except Exception:
        pass

    try:
        sock.close()
    except Exception:
        pass


def receiver(fileobj, stop_event, stats):
    try:
        while not stop_event.is_set():
            line = fileobj.readline()
            if not line:
                return
            stats.inc("recv_lines")
    except Exception:
        return


def spammer(sock, send_lock, stop_event, interval_seconds, stats):
    try:
        while not stop_event.is_set():
            if not send_line(sock, "help", send_lock, stats):
                return
            time.sleep(interval_seconds)
    except Exception:
        return


def worker(index, args, stop_event, stats):
    rnd = random.Random(index * 7919 + int(time.time()))
    props = [p.strip() for p in args.properties.split(",") if p.strip()]
    if not props:
        props = ["DataCorePlugin.CurrentDateTime"]

    while not stop_event.is_set():
        sock = None
        fileobj = None
        recv_stop = threading.Event()
        recv_thread = None
        spam_stop = threading.Event()
        spam_thread = None
        send_lock = threading.Lock()

        try:
            sock = socket.create_connection((args.host, args.port), timeout=args.connect_timeout)
            sock.settimeout(args.socket_timeout)
            fileobj = sock.makefile("r", encoding="utf-8", newline="\n")
            stats.inc("connect_ok")

            try:
                _ = fileobj.readline()
            except Exception:
                pass

            for _ in range(args.subscriptions_per_session):
                p = rnd.choice(props)
                if not send_line(sock, "subscribe " + p, send_lock, stats):
                    raise RuntimeError("subscribe send failed")

            recv_thread = threading.Thread(target=receiver, args=(fileobj, recv_stop, stats), daemon=True)
            recv_thread.start()

            if args.help_spam_interval > 0:
                spam_thread = threading.Thread(
                    target=spammer,
                    args=(sock, send_lock, spam_stop, args.help_spam_interval, stats),
                    daemon=True,
                )
                spam_thread.start()

            session_length = rnd.uniform(args.session_min, args.session_max)
            time.sleep(session_length)
            stats.inc("sessions")

        except Exception:
            stats.inc("connect_fail")
            stats.inc("worker_errors")
            time.sleep(0.2)
        finally:
            spam_stop.set()
            recv_stop.set()

            if fileobj is not None:
                try:
                    fileobj.close()
                except Exception:
                    pass

            if sock is not None:
                abrupt_close(sock)

            if spam_thread is not None:
                spam_thread.join(timeout=0.5)
            if recv_thread is not None:
                recv_thread.join(timeout=0.5)

            time.sleep(rnd.uniform(args.reconnect_min, args.reconnect_max))


def main():
    parser = argparse.ArgumentParser(description="SimHub Property Server reconnect stress client")
    parser.add_argument("--host", default="127.0.0.1")
    parser.add_argument("--port", type=int, default=18082)
    parser.add_argument("--clients", type=int, default=12)
    parser.add_argument("--runtime", type=int, default=1800, help="seconds")
    parser.add_argument("--properties", default="DataCorePlugin.CurrentDateTime")
    parser.add_argument("--subscriptions-per-session", type=int, default=1)
    parser.add_argument("--session-min", type=float, default=2.0)
    parser.add_argument("--session-max", type=float, default=8.0)
    parser.add_argument("--reconnect-min", type=float, default=0.05)
    parser.add_argument("--reconnect-max", type=float, default=0.5)
    parser.add_argument("--help-spam-interval", type=float, default=0.25, help="0 disables")
    parser.add_argument("--connect-timeout", type=float, default=3.0)
    parser.add_argument("--socket-timeout", type=float, default=5.0)
    args = parser.parse_args()

    stop_event = threading.Event()
    stats = Stats()
    threads = []

    for i in range(args.clients):
        t = threading.Thread(target=worker, args=(i, args, stop_event, stats), daemon=True)
        t.start()
        threads.append(t)

    start = time.time()
    try:
        while time.time() - start < args.runtime:
            time.sleep(5)
            s = stats.snapshot()
            elapsed = int(time.time() - start)
            print(
                "[{0:5}s] ok={1} fail={2} sessions={3} send_fail={4} recv_lines={5} worker_err={6}".format(
                    elapsed,
                    s["connect_ok"],
                    s["connect_fail"],
                    s["sessions"],
                    s["send_fail"],
                    s["recv_lines"],
                    s["worker_errors"],
                )
            )
    except KeyboardInterrupt:
        pass
    finally:
        stop_event.set()
        for t in threads:
            t.join(timeout=2.0)


if __name__ == "__main__":
    main()

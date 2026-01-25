// Copyright (C) 2026 Martin Renner
// LGPL-3.0-or-later (see file COPYING and COPYING.LESSER)

using System.Net;

namespace SimHub.Plugins.PropertyServer.Settings
{
    public enum ListenAddress
    {
        Loopback,
        Any
    }

    public static class ListenAddressExtensions
    {
        public static IPAddress ToIpAddress(this ListenAddress address)
        {
            switch (address)
            {
                case ListenAddress.Loopback:
                    return IPAddress.Loopback;
                case ListenAddress.Any:
                    return IPAddress.Any;
                default:
                    return IPAddress.Loopback;
            }
        }
    }
}
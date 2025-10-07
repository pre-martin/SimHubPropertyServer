// v1 Calculates the estimated time remaining until the fuel tank is empty based on current speed and fuel range.

const FUEL_TIME_PROP = 'ComputedPropertiesPlugin.ETS2.FuelTimeRemaining';

function init() {
    createProperty(FUEL_TIME_PROP);
    subscribe('DataCorePlugin.GameRawData.TruckValues.CurrentValues.DashboardValues.FuelValue.Range', 'calculateFuelTime');
}

function calculateFuelTime() {
    // Math.floor() to avoid an excessivly high update frequency
    const fuelDistance = Math.floor(getPropertyValue('DataCorePlugin.GameRawData.TruckValues.CurrentValues.DashboardValues.FuelValue.Range'));
    var speed = Math.floor(getPropertyValue('SpeedKmh'));
    speed = roundUpToFive(speed); // Reduce oscillation by rounding up in increments of 5 (65, 70, 75, 80, ...)
    if (speed < 30) speed = 30;

    const fuelTime = fuelDistance / speed;

    setPropertyValue(FUEL_TIME_PROP, fuelTime);
}

const roundUpToFive = (num) => Math.ceil(num / 5) * 5;

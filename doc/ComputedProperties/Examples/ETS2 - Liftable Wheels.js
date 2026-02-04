// v1 Computes if a truck or trailer has liftable wheels by checking all liftable wheel properties

const TRAILER_LIFTABLE_PROP = 'ComputedPropertiesPlugin.ETS2.TrailerLiftable';
const TRUCK_LIFTABLE_PROP = 'ComputedPropertiesPlugin.ETS2.TruckLiftable';

function init() {
    createProperty(TRAILER_LIFTABLE_PROP);
    subscribe('DataCorePlugin.GameRawData.TrailerValues01.Attached', 'calculateTrailer');
    subscribe('DataCorePlugin.GameRawData.TrailerValues01.LicensePlate', 'calculateTrailer');

    createProperty(TRUCK_LIFTABLE_PROP);
    subscribe('DataCorePlugin.GameRawData.TruckValues.ConstantsValues.LicensePlate', 'calculateTruck');

    setPropertyValue(TRAILER_LIFTABLE_PROP, false);
    setPropertyValue(TRUCK_LIFTABLE_PROP, false);
}

function calculateTrailer() {
    log('Calculating trailer liftable wheels');
    const trailerAttached = getPropertyValue('DataCorePlugin.GameRawData.TrailerValues01.Attached');
    if (!trailerAttached) {
        // No trailer -> no liftable axles
        log('No trailer attached');
        setPropertyValue(TRAILER_LIFTABLE_PROP, false);
        return;
    }

    var isLiftable = false;
    const maxTrailers = getPropertyValue('DataCorePlugin.GameRawData.MaxTrailerCount');
    for (var trailer = 1; trailer <= maxTrailers; trailer++) {
        const trailerNo = zeroPad(trailer, 1);
        const isAttached = getPropertyValue(`DataCorePlugin.GameRawData.TrailerValues${trailerNo}.Attached`);
        if (!isAttached) {
            continue;
        }
        const numberOfWheels = getPropertyValue(`DataCorePlugin.GameRawData.TrailerValues${trailerNo}.WheelsConstant.Count`);
        var liftableCount = 0;
        for (var wheel = 1; wheel <= numberOfWheels; wheel++) {
            const wheelNo = zeroPad(wheel, 1);
            const name = `DataCorePlugin.GameRawData.TrailerValues${trailerNo}.WheelsConstant.Liftable${wheelNo}`;
            const value = getPropertyValue(name);
            if (value === true) liftableCount++;
            isLiftable ||= value;
        }
        log(`Trailer ${trailerNo}: Liftable wheels: ${liftableCount} / ${numberOfWheels}`);
    }

    log(`TrailerLiftable = ${isLiftable}`);
    setPropertyValue(TRAILER_LIFTABLE_PROP, isLiftable);
}

function calculateTruck() {
    log('Calculating truck liftable wheels');

    const numberOfWheels = getPropertyValue('DataCorePlugin.GameRawData.TruckValues.ConstantsValues.WheelsValues.Count');
    var liftableCount = 0;

    var isLiftable = false;
    for (var wheel = 1; wheel <= numberOfWheels; wheel++) {
        const wheelNo = zeroPad(wheel, 1);
        const name = `DataCorePlugin.GameRawData.TruckValues.ConstantsValues.WheelsValues.Liftable${wheelNo}`;
        const value = getPropertyValue(name);
        if (value === true) liftableCount++;
        isLiftable ||= value;
    }

    log(`TruckLiftable = ${isLiftable} (Liftable wheels: ${liftableCount} / ${numberOfWheels})`);
    setPropertyValue(TRUCK_LIFTABLE_PROP, isLiftable);
}

const zeroPad = (num) => num < 10 ? '0' + num : num;

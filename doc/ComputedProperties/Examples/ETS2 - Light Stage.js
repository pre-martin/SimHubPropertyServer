// v1 - Light Stage Control for ETS2. Controls parking lights, low beam and high beam lights.

function init() {
}

function lightDec() {
    const parking = getPropertyValue('GameRawData.TruckValues.CurrentValues.LightsValues.Parking');
    const beamLow = getPropertyValue('GameRawData.TruckValues.CurrentValues.LightsValues.BeamLow');
    const beamHigh = getPropertyValue('GameRawData.TruckValues.CurrentValues.LightsValues.BeamHigh');

    if (parking === true && beamLow === true && beamHigh === true) { // High Beam -> Low Beam
        startRole('RainLight');
        stopRole('RainLight');
    } else if (beamLow === true || parking === true) { // Low Beam -> Off
        startRole('Headlights');
        stopRole('Headlights');
    }
}

function lightInc() {
    const parking = getPropertyValue('GameRawData.TruckValues.CurrentValues.LightsValues.Parking');
    const beamLow = getPropertyValue('GameRawData.TruckValues.CurrentValues.LightsValues.BeamLow');
    const beamHigh = getPropertyValue('GameRawData.TruckValues.CurrentValues.LightsValues.BeamHigh');

    if (parking === false && beamLow === false) { // Off -> Parking
        startRole('Headlights');
        stopRole('Headlights');
    } else if (parking === true && beamLow === false) { // Parking -> Low Beam
        startRole('Headlights');
        stopRole('Headlights');
    } else if (parking === true && beamLow === true && beamHigh === false) { // Low Beam -> High Beam
        startRole('RainLight');
        stopRole('RainLight');
    }
}

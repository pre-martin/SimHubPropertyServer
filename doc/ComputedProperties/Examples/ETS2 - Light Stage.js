// v2 - Light Stage Control for ETS2. Controls parking lights, low beam and high beam lights.

const ROLE_LIGHT_DEC = 'InputStatus.ControlMapperPlugin.Light-';
const ROLE_LIGHT_INC = 'InputStatus.ControlMapperPlugin.Light+';

function init() {
    subscribe(ROLE_LIGHT_DEC, 'lightDec');
    subscribe(ROLE_LIGHT_INC, 'lightInc');
}

function lightDec() {
    if (getPropertyValue(ROLE_LIGHT_DEC) == 0) return;

    const parking = getPropertyValue('GameRawData.TruckValues.CurrentValues.LightsValues.Parking');
    const beamLow = getPropertyValue('GameRawData.TruckValues.CurrentValues.LightsValues.BeamLow');
    const beamHigh = getPropertyValue('GameRawData.TruckValues.CurrentValues.LightsValues.BeamHigh');

    if (parking === true && beamLow === true && beamHigh === true) { // High Beam -> Low Beam
        startRole('RainLight');
        stopRole('RainLight');
    } else if (beamLow === true && parking === true) { // Low Beam -> Off
        startRole('Headlights');
        stopRole('Headlights');
    }
}

function lightInc() {
    if (getPropertyValue(ROLE_LIGHT_INC) == 0) return;

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

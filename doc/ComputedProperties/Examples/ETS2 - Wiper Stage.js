// v1 Calculates the current wiper stage based on input from Control Mapper.

const WIPER_STAGE_PROP = 'ComputedPropertiesPlugin.ETS2.WiperStage';
var wiperStage = 0;

function init() {
    createProperty(WIPER_STAGE_PROP);

    subscribe('InputStatus.ControlMapperPlugin.WiperDec', 'calculate');
    subscribe('InputStatus.ControlMapperPlugin.WiperInc', 'calculate');

    subscribe('DataCorePlugin.GameRawData.TruckValues.ConstantsValues.LicensePlate', 'reset');

    setPropertyValue(WIPER_STAGE_PROP, wiperStage);
}

function calculate() {
    const wiperDec = getPropertyValue('InputStatus.ControlMapperPlugin.WiperDec');
    const wiperInc = getPropertyValue('InputStatus.ControlMapperPlugin.WiperInc');

    if (wiperDec > 0) wiperStage--;
    if (wiperInc > 0) wiperStage++;

    if (wiperStage < 0) wiperStage = 0;
    if (wiperStage > 3) wiperStage = 3;

    setPropertyValue(WIPER_STAGE_PROP, wiperStage);
}

function reset() {
    wiperStage = 0;
    setPropertyValue(WIPER_STAGE_PROP, wiperStage);
}

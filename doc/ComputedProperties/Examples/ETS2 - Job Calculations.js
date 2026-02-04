// v1 Calulates the time buffer (in minutes) for the current job.

const JOB_BUFFER_PROP = 'ComputedPropertiesPlugin.ETS2.JobBuffer';

function init() {
    createProperty(JOB_BUFFER_PROP);

    subscribe('GameRawData.CommonValues.NextRestStop.Minutes', 'calculateJobBuffer');
    subscribe('GameRawData.NavigationValues.NavigationTimeSeconds', 'calculateJobBuffer');
    subscribe('GameRawData.JobValues.RemainingDeliveryTime.Minutes', 'calculateJobBuffer');
}

const DRIVE_DURATION = 11 * 60; // 11 hours in minutes
const REST_DURATION = 9 * 60;   // 9 hours in minutes

function calculateJobBuffer() {
    const navTime = Math.floor(getPropertyValue('GameRawData.NavigationValues.NavigationTimeSeconds') / 60);
    if (navTime == 0) {
        // No destination set, no nav time -> no buffer
        setPropertyValue(JOB_BUFFER_PROP, 0);
        return;
    }
    const nextRestStop = getPropertyValue('GameRawData.CommonValues.NextRestStop.Minutes');
    var totalTravelTime = 0;

    if (navTime <= nextRestStop) {
        // No rest stop needed
        totalTravelTime = navTime;
    } else {
        // Rest stop(s) needed
        const requiredPauses = Math.ceil((navTime - nextRestStop) / DRIVE_DURATION);
        totalTravelTime = navTime + requiredPauses * REST_DURATION;
    }

    const remainingDelivery = getPropertyValue('GameRawData.JobValues.RemainingDeliveryTime.Minutes');
    const remainingAtArrival = remainingDelivery - totalTravelTime;

    setPropertyValue(JOB_BUFFER_PROP, remainingAtArrival);
}

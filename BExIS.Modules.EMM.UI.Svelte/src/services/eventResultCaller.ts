import { Api } from '@bexis2/bexis2-core-ui';

export const getEvents = async () => {

    let url = "/EMM/EventRegistrationResult/GetEvents";
    
    const res = await Api.get(url);

    return res.data;
}

export const getEventResults = async (eventId: number) => {

    let url = "/EMM/EventRegistrationResult/GetEventRegistrations/" + eventId;

    const res = await Api.get(url);

    return res.data;
}


export const getEventWaitingListResults = async (eventId: number) => {

    let url = "/EMM/EventRegistrationResult/GetWaitingListRegistrations/" + eventId;

    const res = await Api.get(url);

    return res.data;
}

export const deleteEvent = async (id) => {
    try {
        console.log("deleteEvent called with id:", id);
		const response = await Api.get('/EMM/EventRegistrationResult/DeleteAll/' + id);
		return response.data;
	} catch (error) {
		console.error(error);
		throw error;
	}
}

export const deleteRegistration = async (id) => {
    try {
        console.log("deleteRegistration called with id:", id);
		const response = await Api.get('/EMM/EventRegistrationResult/Delete/' + id);
		return response.data;
	} catch (error) {
		console.error(error);
		throw error;
	}
}


export const moveEventRegistration = async (id) => {

    try {
		const response = await Api.get('/EMM/EventRegistrationResult/MoveFromWaitingList/' + id);
		return response.data;
	} catch (error) {
		console.error(error);
		throw error;
	}
}

export const Resend = async (id) => {

	console.log("Resend called with id:", id);
    try {
		const response = await Api.get('/EMM/EventRegistrationResult/ResendNotification/' + id );
		return response.data;
	} catch (error) {
		console.error(error);
		throw error;
	}
}

export const clearEvent = async (id) => {

    try {
        const response = await Api.get('/EMM/EventRegistrationResult/Clear/' + id);
		return response.data;
	} catch (error) {
		console.error(error);
		throw error;
	}
}



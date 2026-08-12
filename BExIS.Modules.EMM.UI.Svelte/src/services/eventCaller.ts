import { Api } from '@bexis2/bexis2-core-ui';
import type { EditEvent } from '../models/eventModels';
// Fetch data from API and return as JSON object
export const getEvents = async () => {

    let url = "/EMM/Events/GetEvents";
    
    const res = await Api.get(url);
    console.log("events:", res.data);
    
    return res.data;
}

export const getEvent = async (id) => {

    let url = "/EMM/Events/Get/" + id;
    
    const res = await Api.get(url);

    
       return res.data;
}

export const saveEvent = async (event: EditEvent) => {
	try {
		const response = await Api.post('/EMM/Events/Create', event);
		return response.data;
	} catch (error) {
		console.error(error);
		throw error;
	}
};

export const updateEvent = async (event: EditEvent) => {
	try {
		const response = await Api.post('/EMM/Events/Update', event);
		return response.data;
	} catch (error) {
		console.error(error);
		throw error;
	}
};

export const deleteEvent = async (id) => {
	try {
        console.log("deleteEvent called with id:", id);
        // Ensure id is a number or string that can be converted to a number
		const response = await Api.post('/EMM/Events/Delete', { id });
		return response.data;
	} catch (error) {
		console.error(error);
		throw error;
	}
};

import { Api } from '@bexis2/bexis2-core-ui';
import { setApiConfig } from '@bexis2/bexis2-core-ui';
import type { EditEvent } from '../models/eventModels';
// Fetch data from API and return as JSON object
export const getEvents = async () => {
  
    setApiConfig('http://localhost:44345/', 'epetzold', '2021.B2.Go$On');
    let url = "/EMM/Event/GetEvents";
    
    const res = await Api.get(url);
    console.log("events:", res.data);
    
    return res.data;
}

export const getEvent = async (id) => {
  
    setApiConfig('http://localhost:44345/', 'epetzold', '2021.B2.Go$On');
    let url = "/EMM/Event/Get/" + id;
    
    const res = await Api.get(url);

    
       return res.data;
}

export const saveEvent = async (event: EditEvent) => {
	console.log("saveEvent called with:", event);
    setApiConfig('http://localhost:44345/', 'epetzold', '2021.B2.Go$On');
	try {
		const response = await Api.post('/EMM/Event/Create', event);
		return response.data;
	} catch (error) {
		console.error(error);
		throw error;
	}
};

export const updateEvent = async (event: EditEvent) => {
    setApiConfig('http://localhost:44345/', 'epetzold', '2021.B2.Go$On');
	try {
		const response = await Api.post('/EMM/Event/Update', event);
		return response.data;
	} catch (error) {
		console.error(error);
		throw error;
	}
};

export const deleteEvent = async (id) => {
    setApiConfig('http://localhost:44345/', 'epetzold', '2021.B2.Go$On');
	try {
        console.log("deleteEvent called with id:", id);
        // Ensure id is a number or string that can be converted to a number
		const response = await Api.post('/EMM/Event/Delete', { id });
		return response.data;
	} catch (error) {
		console.error(error);
		throw error;
	}
};

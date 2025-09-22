import { Api } from '@bexis2/bexis2-core-ui';
import { setApiConfig } from '@bexis2/bexis2-core-ui';

export const getEvents = async () => {
  
    setApiConfig('http://localhost:44345/', 'epetzold', '2021.B2.Go$On');
    let url = "/EMM/EventRegistrationResult/GetEvents";
    
    const res = await Api.get(url);

    return res.data;
}

export const getEventResults = async (eventId: number) => {
  
    setApiConfig('http://localhost:44345/', 'epetzold', '2021.B2.Go$On');
    let url = "/EMM/EventRegistrationResult/GetEventRegistrations/" + eventId;

    const res = await Api.get(url);

    return res.data;
}

export const deleteEvent = async (id) => {
    setApiConfig('http://localhost:44345/', 'epetzold', '2021.B2.Go$On');
	
    try {
        console.log("deleteEvent called with id:", id);
		const response = await Api.post('/EMM/EventRegistrationResult/Delete', { id });
		return response.data;
	} catch (error) {
		console.error(error);
		throw error;
	}
}


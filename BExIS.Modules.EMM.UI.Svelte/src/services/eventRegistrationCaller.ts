import { Api } from '@bexis2/bexis2-core-ui';
import { setApiConfig } from '@bexis2/bexis2-core-ui';
import type { EventRegistration } from '../models/eventregistrationModel';


export const getEvents = async () => {
  
    setApiConfig('http://localhost:44345/', 'epetzold', '2021.B2.Go$On');
    let url = "/EMM/EventRegistration/GetEvents";
    
    const res = await Api.get(url);
    console.log("events:", res.data);
    
    return res.data;
}

export const getEventRegistrationJson = async (eventId: number) => {
  
    setApiConfig('http://localhost:44345/', 'epetzold', '2021.B2.Go$On');
    let url = "/EMM/EventRegistration/GetEventRegistrationJson/" + eventId;

    const res = await Api.get(url);

    
    return res.data;
}


export const saveEventRegistration = async (eventReg: EventRegistration) => {
    setApiConfig('http://localhost:44345/', 'epetzold', '2021.B2.Go$On');
    try {
        const response = await Api.post('/emm/EventRegistration/Create', eventReg);
        return response.data;
    } catch (error) {
        console.error(error);
        
        throw error;
    }
};

export const getEventRegistration = async (id: number) => {
    setApiConfig('http://localhost:44345/', 'epetzold', '2021.B2.Go$On');
    try {
        const response = await Api.get(`/emm/EventRegistration/Get/${id}`);
        return response.data;
    } catch (error) {
        console.error(error);
        throw error;
    }
};

export const getEventRegistrationPassword = async (id: number) => {
    setApiConfig('http://localhost:44345/', 'epetzold', '2021.B2.Go$On');
    try {
        const response = await Api.get(`/emm/EventRegistration/GetEventPassword/${id}`);
        return response.data;
    } catch (error) {
        console.error(error);
        throw error;
    }
};

export const editEventRegistration = async (eventReg: EventRegistration) => {
    setApiConfig('http://localhost:44345/', 'epetzold', '2021.B2.Go$On');
    try {
        const response = await Api.post('/emm/EventRegistration/Edit', eventReg);
        return response.data;
    } catch (error) {
        console.error(error);
        throw error;
    }
};

export const deleteEventRegistration = async (id: number) => {
    setApiConfig('http://localhost:44345/', 'epetzold', '2021.B2.Go$On');
    try {
        const response = await Api.get(`/emm/EventRegistration/Delete/${id}`);
        return response.data;
    } catch (error) {
        console.error(error);
        throw error;
    }
};

export const userAllreadyRegister = async (eventReg: EventRegistration) => {
    setApiConfig('http://localhost:44345/', 'epetzold', '2021.B2.Go$On');
    try {
        const response = await Api.post(`/emm/EventRegistration/UserAlreadyRegistered/`, eventReg);
        return response.data;
    } catch (error) {
        console.error(error);
        throw error;
    }
};

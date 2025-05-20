import { Api } from '@bexis2/bexis2-core-ui';
import { setApiConfig } from '@bexis2/bexis2-core-ui';

// Fetch data from API and return as JSON object
export const getEvents = async () => {
  
    //setApiConfig('http://localhost:44345/', 'epetzold', '2021.B2.Go$On');
    let url = "/EMM/Event/GetEvents";
    
    const res = await Api.get(url);

    let result = await res.data;
       return result;
}
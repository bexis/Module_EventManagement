import { setApiConfig } from '@bexis2/bexis2-core-ui';

/** @type {import('./$types').PageLoad} */
export function load() {
	if (import.meta.env.DEV) {
		console.log('dev');
		setApiConfig('http://localhost:44345/', 'epetzold', '2021.B2.Go$On');
	}

	return {};
}
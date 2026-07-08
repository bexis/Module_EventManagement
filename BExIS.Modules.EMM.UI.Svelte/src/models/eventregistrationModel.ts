// Generic interface
export interface EventRegistration{
  eventId: number;
  jsonFile: string;   
}


export class ReadEventModel {
  id: string;
  name: string;
  description: string;
  entries: Array<ReadEntryModel> = [];

	constructor(json: any) {
		this.id = json.id;
		this.name = json.name;
		this.description = json.description;

		json.entries.forEach((entry) => {
			this.entries.push(entry);
		});
	}
}

export class EventRegistrationLoadModel
{
	name: string;
	date: string;
	location: string;
	language: string;
	importantInformation: string;
	jsonFile: string;

	constructor(json: any) {
		this.name = json.name;
		this.date = json.date;
		this.location = json.location;
		this.language = json.language;
		this.importantInformation = json.importantInformation;
		this.jsonFile = json.jsonFile;

		
	}
}



export class UpdateEventRegistrationModel {
	registration: Array<UpdateSectionModel>;
	//entries: Array<UpdateEntryModel>;

	constructor(json: any) {

		this.registration = new Array<UpdateSectionModel>();

		 Object.values(json.registration).forEach((section) => {
			this.registration.push(new UpdateSectionModel(section));
		});
	}
}

export class UpdateSectionModel{
	title: string;
	entries: Array<UpdateEntryModel>;

	constructor(json: any) {
		this.title = json.title;
		this.entries = new Array<UpdateEntryModel>();

		json.entries.forEach((entry) => {
			this.entries.push(new UpdateEntryModel(entry));
		});
	}
}

export class ReadEntryModel {
	key: string;
	title: string;
	value: any;
	type: string;
	description: string;
	options: string[];
	required?: boolean;

	constructor(json: any) {
		this.key = json.key;
		this.title = json.title;
		this.value = json.value;
		this.type = json.type;
		this.description = json.description;
		this.options = json.options;
		this.required = json.required;
	}
}

export class UpdateEntryModel {
	key: string;
	title: string;
	value: any;
	type: string;
	description: string;
	options: string[];

	constructor(json: any) {
		this.key = json.key;
		this.title = json.title;
		this.type = json.type;
		this.description = json.description;
		this.options = json.options;
		this.value = JSON.stringify(json.value);
	}
}
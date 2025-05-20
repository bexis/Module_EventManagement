
export interface EventListItem {
	id: number;
	name: string;
	participantsLimitation: number;
	abbreviation: string;
	startDate: Date;
	endDate: Date;
	allowEdit: boolean;
}



export class ReadEventModel {
	id: string;
	name: string;
	description: string;
	entries: Array<ReadEntryModel>;

	constructor(json: any) {
		this.id = json.id;
		this.name = json.name;
		this.description = json.description;

		json.entries.forEach(function (entry) {
			this.entries.push(entry);
		});
	}
}

export class UpdateEventModel {
	name: string;
	description: string;
	entries: Array<UpdateEntryModel>;

	constructor(json: any) {
		this.name = json.name;
		this.description = json.description;

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

	constructor(json: any) {
		this.key = json.key;
		this.title = json.title;
		this.value = json.value;
		this.type = json.type;
		this.description = json.description;
		this.options = json.options;
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

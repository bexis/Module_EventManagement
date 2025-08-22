
export interface EventListItem {
	id: number;
	name: string;
	participants: string;
	startDate: string;
	deadline: string;
	editAllowed: boolean;
	alreadyRegistered: boolean;
	inUse: boolean;
	deleted: boolean;

}

export interface EditEvent {
	id: number;
	name: string;
	eventDate: string;
	importantInformation: string;
	location: string;
	mailInformation: string;
	selectedEventLanguage: string;
	jsonFile: string;
	startDate: string;
	deadline: string;
	participantsLimitation: number;
	waitingList: boolean;
	waitingListLimitation: number;
	editAllowed: boolean;
	closed: boolean;
	logInPassword: string;
	emailBCC: string;
	emailCC: string;
	emailReply: string;
	javaScriptPath: string;
	inUse: boolean;
	editAccess: string;
	jsonsKeys:string[];
	jsonKeyEmail:string;
	jsonKeyFirstName:string;
	jsonKeyLastName:string;
}




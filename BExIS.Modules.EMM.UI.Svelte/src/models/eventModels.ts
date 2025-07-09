
export interface EventListItem {
	id: number;
	name: string;
	participantsLimitation: number;
	abbreviation: string;
	startDate: Date;
	endDate: Date;
	allowEdit: boolean;
}

export interface EditEvent {
	Id: number;
	Name: string;
	EventDate: string;
	ImportantInformation: string;
	Location: string;
	MailInformation: string;
	SelectedEventLanguage: string;
	JsonFile: File;
	StartDate: string;
	Deadline: string;
	ParticipantsLimitation: number;
	WaitingList: boolean;
	WaitingListLimitation: number;
	EditAllowed: boolean;
	Closed: boolean;
	LogInPassword: string;
	EmailBCC: string;
	EmailCC: string;
	EmailReply: string;
	JavaScriptPath: string;
	InUse: boolean;
	EditAccess: string;
	JsonsKeys:string[];
	JsonKeyEmail:string;
	JsonKeyFirstName:string;
	JsonKeyLastName:string;
}




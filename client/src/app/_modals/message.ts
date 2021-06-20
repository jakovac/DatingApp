export interface Message {
    id: number;
    senderID: number;
    senderUsername: string;
    senderPhotoUrl: string;
    recipientID: number;
    recipientUsername: string;
    recipientPhotoUrl: string;
    content: string;
    dateRead?: Date;
    messageSent: Date;
}


import { group } from '@angular/animations';
import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { HubConnection, HubConnectionBuilder } from '@microsoft/signalr';
import { BehaviorSubject } from 'rxjs';
import { take } from 'rxjs/operators';
import { environment } from 'src/environments/environment';
import { Group } from '../_modals/group';
import { Message } from '../_modals/message';
import { User } from '../_modals/user';
import { BusyService } from './busy.service';
import { getPaginatedResults, getPaginationHeader } from './paginationHelper';

@Injectable({
  providedIn: 'root'
})
export class MessageService {
  baseUrl = environment.apiUrl;
  hubUrl = environment.hubUrl;
  private hubConnection!: HubConnection;
  //sta se dogadja kada se konektujemo na hub i primimo poruke...prvo kreiramo observable
  private messageThreadSource = new BehaviorSubject<Message[]>([]);
  messageThread$ = this.messageThreadSource.asObservable();

  constructor(private http: HttpClient, private busyService: BusyService) { }

  createHubConnection(user: User, otherUsername: string) {
    this.busyService.busy();
    this.hubConnection = new HubConnectionBuilder()
      .withUrl(this.hubUrl + 'message?user=' + otherUsername, {
        accessTokenFactory: () => user.token
      })
      .withAutomaticReconnect()
      .build()

      this.hubConnection.start()
        .catch(error => console.log(error))
        .finally(() => this.busyService.idle());

      this.hubConnection.on("ReceiveMessageThread", messages => {
        this.messageThreadSource.next(messages);
      })

      this.hubConnection.on("NewMessage", message => {
        this.messageThread$.pipe(take(1)).subscribe(messages => {
          this.messageThreadSource.next([...messages, message])
        })
      })

      this.hubConnection.on("UpdatedGroup", (group: Group) => {
        if(group.connections.some(x => x.username == otherUsername)) {
          this.messageThread$.pipe(take(1)).subscribe(messages => {
            messages.forEach(message => {
              if(!message.dateRead) {
                message.dateRead = new Date(Date.now())
              }
            })
            this.messageThreadSource.next([...messages]);
          })
        }
      })
  }

  stopHubConnection() {
    if(this.hubConnection) {
      this.messageThreadSource.next([]);
      this.hubConnection.stop();
    }
  }

  getMessages(pageNumber: number, pageSize: number, container: string) {
    let params = getPaginationHeader(pageNumber, pageSize);
    params = params.append('Container', container);
    return getPaginatedResults<Message[]>(this.baseUrl + 'messages', params, this.http);
  }

  getMessageThread(username: string) {
    return this.http.get<Message[]>(this.baseUrl + 'messages/thread/' + username);
  }

  async sendMessage(username: string, content: string) {
    //return this.http.post<Message>(this.baseUrl + 'messages', {recipientUsername: username, content})
    return this.hubConnection.invoke('SendMessage', {recipientUsername: username, content}) //vise ne vracamo observable vec promise
      .catch(error => console.log(error)); 
  }

  deleteMessage(ID: number) {
    return this.http.delete(this.baseUrl + 'messages/' + ID);
  }
}
import { Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { HubConnection, HubConnectionBuilder } from '@microsoft/signalr';
import { ToastrService } from 'ngx-toastr';
import { BehaviorSubject } from 'rxjs';
import { take } from 'rxjs/operators';
import { environment } from 'src/environments/environment';
import { User } from '../_modals/user';

@Injectable({
  providedIn: 'root'
})
export class PresenceService {
  hubUrl = environment.hubUrl;
  private hubConnection!: HubConnection;

  private onlineUserSource = new BehaviorSubject<string[]>([]) //niz usernameova
   //pravim observable
  onlineUsers$ = this.onlineUserSource.asObservable(); //sada pravimo novi listening event

  constructor(private toastr: ToastrService, private router: Router) { }

  //pravimo metodu koja kreira hub konekciju, kada se user loguje i kada je autentifikovan onda se kreira hub konekcija na hub

  createHubConnection(user: User) {//treba da posaljemo jwt token kada pravimo konekciju
    this.hubConnection = new HubConnectionBuilder()
      .withUrl(this.hubUrl + 'presence', { //isto ime koje smo koristi kao endpoint za hub
        accessTokenFactory: () => user.token //iz accessTokenFactory cupamo token
      }) 
      .withAutomaticReconnect()
      .build()

    this.hubConnection //startujemo konekciju
      .start()
      .catch(error => console.log(error));

    //sada slusamo server events da li je user online metodu ili offline metodu
      this.hubConnection.on('UserIsOnline', username => {
        // this.toastr.info(username + ' has connected'); //treba nam samo da update-ujemo online usere
        this.onlineUsers$.pipe(take(1)).subscribe(usernames => {
          this.onlineUserSource.next([...usernames, username])
        })
      })

      this.hubConnection.on('UserIsOffline', username => {
        // this.toastr.warning(username + ' has disconnected');
        this.onlineUsers$.pipe(take(1)).subscribe(usernames => {
          this.onlineUserSource.next([...usernames.filter(x => x !== username)]) //uklanja ga iz liste online usera
        })
      })

      this.hubConnection.on('GetOnlineUsers', (usernames: string[]) => {
        this.onlineUserSource.next(usernames); //u member card i u member details koponente da dodamo ako je user online
      })

      this.hubConnection.on('NewMessageReceived',({username, knownAs}) => {
        this.toastr.info(knownAs + ' has sent you a new message!')
          .onTap
          .pipe(take(1))
          .subscribe(() => this.router.navigateByUrl('/members/' + username + '?tab=3'));
      })
  }

  stopHubConnection() {
    this.hubConnection.stop().catch(error => console.log(error));
  }
}
  //znaci kada se aplikacija pokrene i kada je user logovan onda pozivamo createHubConnection, a ako ide logout onda stopHubConnection
  //takodje hocemo da kreiramo createHubConnection kada se user registruje ili kada se loguje - prvo idemo u app.components

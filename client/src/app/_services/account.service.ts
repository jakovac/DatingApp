import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { ReplaySubject } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from 'src/environments/environment';
import { User } from '../_modals/user';

@Injectable({
  providedIn: 'root'
})
export class AccountService {
  baseUrl = environment.apiUrl;
  ///zasto smo ovo setovali da bude observable, da bi moglo da se 
  ///prati od strane drugih klasa ili komponenata
  ///kada se bilo sta subscribe na ovo bice primeceno kada se promeni.
  ///AuthGuard je sad u mogucnosti da se subscribe-uje na ovaj observable
  ///guard se automatski subscribe-uje kada pokusamo da pristupimo currentUserSource
  private currentUserSource = new ReplaySubject<User>(1); 
  currentUser$ = this.currentUserSource.asObservable();

  constructor(private http: HttpClient) { }

  login(model: any) {
    return this.http.post<User>(this.baseUrl + 'account/login', model).pipe(
      map((response: User) => {
        const user = response;
        if(user) {
          this.setCurrentUser(user);
          // localStorage.setItem('user', JSON.stringify(user));
          // this.currentUserSource.next(user);
        }
      })
    )
  }

  register(model: any) {
    return this.http.post<User>(this.baseUrl + 'account/register', model).pipe(
      map((user : User) => {
        if(user) {
          this.setCurrentUser(user);
          // localStorage.setItem('user', JSON.stringify(user));
          // this.currentUserSource.next(user);
        }
        // return user;
      })
    )
  }

  setCurrentUser(user: User) {
    user.roles = [];
    const roles = this.getDecodecToken(user.token).role;
    Array.isArray(roles) ? user.roles = roles : user.roles.push(roles);
    localStorage.setItem('user', JSON.stringify(user));
    this.currentUserSource.next(user);
  }

  logout() {
    localStorage.removeItem('user');
    this.currentUserSource.next(null!);
  }

  getDecodecToken(token: string) {
    return JSON.parse(atob(token.split('.')[1]));
  }
}

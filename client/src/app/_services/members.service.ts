import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { of } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from 'src/environments/environment';
import { Member } from '../_modals/member';

// const httpOptions = {
//   headers: new HttpHeaders ({
//     Authorization: 'Bearer ' + JSON.parse(localStorage.getItem('user') || '{}')?.token
//   })
// }

@Injectable({
  providedIn: 'root'
})
export class MembersService {
  baseUrl = environment.apiUrl;
  members: Member[] = [];

  constructor(private http: HttpClient) { }

  getMembers() {
    if(this.members.length > 0) return of(this.members); //kao observable
    return this.http.get<Member[]>(this.baseUrl + 'users'/*, httpOptions*/).pipe(
      map(members => {
        this.members = members;
        return members; //map vraca kao observable
      })
    )
  }

  getMember(username: string) {
    const member = this.members.find(x => x.username === username);
    if(member !== undefined) return of(member);
    return this.http.get<Member>(this.baseUrl + 'users/' + username/*, httpOptions*/);
  }

  updateMember(member: Member) {
    return this.http.put(this.baseUrl + 'users', member).pipe(
      map(() =>  {
        const index = this.members.indexOf(member);
        this.members[index] = member;
      })
    )
  }
}

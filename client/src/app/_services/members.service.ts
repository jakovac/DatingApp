import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { of, pipe } from 'rxjs';
import { map, take } from 'rxjs/operators';
import { environment } from 'src/environments/environment';
import { Member } from '../_modals/member';
import { PaginatedResult } from '../_modals/pagination';
import { User } from '../_modals/user';
import { UserParams } from '../_modals/userParams';
import { AccountService } from './account.service';
import { getPaginatedResults, getPaginationHeader } from './paginationHelper';

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
  memberCache = new Map();
  user!: User;
  userParams!: UserParams;

  constructor(private http: HttpClient, private accountService: AccountService) { 
    this.accountService.currentUser$.pipe(take(1)).subscribe(user => {
      this.user = user;
      this.userParams = new UserParams(user);
    })
  }

  getUserParams() {
    return this.userParams
  }
  setUserParams(params : UserParams) {
    this.userParams = params;
  }
  resetUserParams() {
    this.userParams = new UserParams(this.user)
    return this.userParams;
  }

  getMembers(userParams: UserParams/*page?: number, itemsPerPage?: number*/) {
    // console.log(Object.values(userParams).join('-'))
    var response = this.memberCache.get(Object.values(userParams).join('-'));
    if(response) {
      return of(response);
    }

    let params = getPaginationHeader(userParams.pageNumber, userParams.pageSize);

    params = params.append('minAge', userParams.minAge.toString());
    params = params.append('maxAge', userParams.maxAge.toString());
    params = params.append('gender', userParams.gender);
    params = params.append('orderBy', userParams.orderBy);
    // if(page !== null && itemsPerPage !== null) {
    //   params = params.append('pageNumber', page!.toString());
    //   params = params.append('pageSize', itemsPerPage!.toString());
    // }

    return getPaginatedResults<Member[]>(this.baseUrl + 'users', params, this.http)
      .pipe(map(response => {
        this.memberCache.set(Object.values(userParams).join('-'), response);
        return response;
      }))
   
    //if(this.members.length > 0) return of(this.members); //kao observable
    //return this.http.get<Member[]>(this.baseUrl + 'users'/*, httpOptions*/).pipe(
      // map(members => {
      //   this.members = members;
      //   return members; //map vraca kao observable
      // })
    // )
  }

  getMember(username: string) {
    // const member = this.members.find(x => x.username === username);
    // if(member !== undefined) return of(member);
    //console.log(this.memberCache);
    const member = [...this.memberCache.values()]
    // console.log(member);
    .reduce((arr,elem) => arr.concat(elem.result), [])
    .find((member: Member) => member.username === username);

    if(member) {
      return of(member);
    }
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

  setMainPhoto(photoId: number) {
    return this.http.put(this.baseUrl + 'users/set-main-photo/' + photoId, {});
  }

  deletePhoto(photoId: number) {
    return this.http.delete(this.baseUrl + 'users/delete-photo/' + photoId);
  }

  addLike(username: string) {
    return this.http.post(this.baseUrl + 'likes/' + username, {});
  }

  getLikes(predicate: string, pageNumber: number, pageSize: number) {
    let params = getPaginationHeader(pageNumber, pageSize);
    params = params.append('predicate', predicate);
    return getPaginatedResults<Partial<Member[]>>(this.baseUrl + 'likes', params, this.http);//this.http.get<Partial<Member[]>>(this.baseUrl + 'likes?predicate=' + predicate);
  }

}

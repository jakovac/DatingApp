import { Component, HostListener, OnInit, ViewChild } from '@angular/core';
import { NgForm } from '@angular/forms';
import { ToastrService } from 'ngx-toastr';
import { take } from 'rxjs/operators';
import { Member } from 'src/app/_modals/member';
import { User } from 'src/app/_modals/user';
import { AccountService } from 'src/app/_services/account.service';
import { MembersService } from 'src/app/_services/members.service';

@Component({
  selector: 'app-member-edit',
  templateUrl: './member-edit.component.html',
  styleUrls: ['./member-edit.component.css']
})
//prvo da uzmemo current usera preko account servisa i onda cemo odatle uzeti username da uzmemo tok konkretnog membera
export class MemberEditComponent implements OnInit {
  @ViewChild('editForm') editForm?: NgForm;
  member?: Member;
  user?: User;
  //dakle punimo User objekat iz current usera iz AccountSetvice-a, a current user name je observable,
  //moramo da izvadimo usera iz observable-a
  @HostListener('window:beforeunload', ['$event']) unloadNotification($event: any) { //pristup browser elementima
    if(this.editForm?.dirty) {
      $event.returnValue = true;
    }
  } 
  constructor(private accountService: AccountService, private memberService: MembersService, 
      private toastr: ToastrService) { 
        this.accountService.currentUser$.pipe(take(1)).subscribe(user => this.user = user) //ovako je nas user napunjen sa current userom iz account servisa
  }

  ngOnInit(): void {
    this.loadMember();
  }

  loadMember() {
    this.memberService.getMember(this.user?.username!).subscribe(member => {
      this.member = member
    })
  }

  updateMember() {
    this.memberService.updateMember(this.member!).subscribe(() => {
      this.toastr.success('Profile updated succesefully');
      this.editForm?.reset(this.member);//updated member posle submita forme
    })
    
  }

}

import { Component, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { MembersService } from 'src/app/_services/members.service';
import { ActivatedRoute, Router } from '@angular/router';
import { NgxGalleryOptions, NgxGalleryImage, NgxGalleryAnimation } from '@kolkov/ngx-gallery';
import { Member } from 'src/app/_modals/member';
import { TabDirective, TabsetComponent } from 'ngx-bootstrap/tabs';
import { Message } from 'src/app/_modals/message';
import { MessageService } from 'src/app/_services/message.service';
import { PresenceService } from 'src/app/_services/presence.service';
import { AccountService } from 'src/app/_services/account.service';
import { User } from 'src/app/_modals/user';
import { take } from 'rxjs/operators';

@Component({
  selector: 'app-member-detail',
  templateUrl: './member-detail.component.html',
  styleUrls: ['./member-detail.component.css']
})
export class MemberDetailComponent implements OnInit, OnDestroy {
  @ViewChild('memberTabs', {static: true}) memberTabs!: TabsetComponent;
  member?: Member;
  galleryOptions?: NgxGalleryOptions[];
  galleryImages?: NgxGalleryImage[];
  activeTab!: TabDirective;
  messages: Message[] = [];
  user!: User;

  constructor(public presence: PresenceService, private route: ActivatedRoute, private messageService: MessageService,
      private accountService: AccountService, private router: Router) { 
        this.accountService.currentUser$.pipe(take(1)).subscribe(user => this.user = user);
        this.router.routeReuseStrategy.shouldReuseRoute = () => false; //treba nam ruter za situaciju
        //kada je user na drugom profilu i kada mu stigne notifikacija da mu je neko poslao poruku redirektuje ga na taj profil na tab sa porukama
        //ali ne vidi poruke, kao da ih nema
      }

  ngOnInit(): void {
    this.route.data.subscribe(data => {
      this.member = data.member;
    })

    this.route.queryParams.subscribe(params => {
      params.tab ? this.selectTab(params.tab) : this.selectTab(0);
    })

    this.galleryOptions = [
      {
        width: '500px',
        height: '500px',
        imagePercent: 100,
        thumbnailsColumns: 4,
        imageAnimation: NgxGalleryAnimation.Slide,
        preview: false
      }
    ]
    this.galleryImages = this.getImages();
  }

  getImages(): NgxGalleryImage[] {
    const imageUrls = [];
    for (const photo of this.member!.photos) {
      imageUrls.push({
        small: photo?.url,
        medium: photo?.url,
        big: photo?.url
      })
    }
    return imageUrls;
  }

  // loadMember() {
  //   this.memberService.getMember(this.route.snapshot.paramMap.get('username')!).subscribe(member => {
  //     this.member = member;
      
  //   })
  // }

  loadMessages() {
    this.messageService.getMessageThread(this.member!.username).subscribe(messages => {
      this.messages = messages;
    })
  }

  selectTab(tabID: number) {
    this.memberTabs.tabs[tabID].active = true;
  }

  onTabActivated(data: TabDirective) {
    this.activeTab = data;
    if(this.activeTab.heading === 'Messages' && this.messages.length === 0) {
      this.messageService.createHubConnection(this.user, this.member!.username)
      // this.loadMessages();
    } else { //ako ode sa messages taba onda se diskonektuje sa haba
      this.messageService.stopHubConnection();
      //takodje treba da se diskonektuje ako potpuno ode sa kompnente - za to koristimo OnDestroy

    }
    //sada odavde treba da prosledimo poruke u child komponente member-details.componente
  }

  ngOnDestroy(): void {
    this.messageService.stopHubConnection();
    //u funkciji stopHubConnection treba da dodamo uslov ako je vec stopirana konekcija da ne radi nista
  }
}
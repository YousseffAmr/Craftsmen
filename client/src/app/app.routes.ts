import { Routes } from '@angular/router';
import { CraftAdminComponent } from './craft-admin.component';
import { LoginComponent } from './login.component';
import { SignupComponent } from './signup.component';
import { authGuard } from './auth.guard';
import { adminGuard } from './admin.guard';
import { CraftsmanAdminComponent } from './craftsman-admin.component';
import { CraftsmanCraftsComponent } from './craftsman-crafts.component';
import { craftsmanGuard } from './craftsman.guard';
import { CustomerCraftsmenComponent } from './customer-craftsmen.component';
import { CustomerRequestComponent } from './customer-request.component';
import { customerGuard } from './customer.guard';
import { homeGuard } from './home.guard';
import { CraftsmanRequestsComponent } from './craftsman-requests.component';
import { LandingComponent } from './landing.component';
import { CraftsmanBoardComponent } from './craftsman-board.component';
import { CraftsmanDaySheetComponent } from './craftsman-day-sheet.component';
import { CustomerRequestsComponent } from './customer-requests.component';
import { CraftsmanProfileComponent } from './craftsman-profile.component';
import { CustomerProfileComponent } from './customer-profile.component';
import { CraftsmanNotificationsComponent } from './craftsman-notifications.component';
import { CustomerNotificationsComponent } from './customer-notifications.component';
import { CraftsmanStatusComponent } from './craftsman-status.component';

export const appRoutes: Routes = [
  { path: '', component: LandingComponent, pathMatch: 'full' },
  { path: 'signup', component: SignupComponent },
  { path: 'login', component: LoginComponent },
  { path: 'home', component: CraftAdminComponent, canActivate: [homeGuard] },
  { path: 'admin/craftsmen', component: CraftsmanAdminComponent, canActivate: [authGuard, adminGuard] },
  { path: 'craftsman/crafts', component: CraftsmanCraftsComponent, canActivate: [craftsmanGuard] },
  { path: 'craftsman/requests', component: CraftsmanRequestsComponent, canActivate: [craftsmanGuard] },
  { path: 'craftsman/board', component: CraftsmanBoardComponent, canActivate: [craftsmanGuard] },
  { path: 'craftsman/day-sheet', component: CraftsmanDaySheetComponent, canActivate: [craftsmanGuard] },
  { path: 'craftsman/notifications', component: CraftsmanNotificationsComponent, canActivate: [craftsmanGuard] },
  { path: 'craftsman/profile', component: CraftsmanProfileComponent, canActivate: [craftsmanGuard] },
  { path: 'craftsman/status', component: CraftsmanStatusComponent, canActivate: [craftsmanGuard] },
  { path: 'customer/craftsmen', component: CustomerCraftsmenComponent, canActivate: [customerGuard] },
  { path: 'customer/craftsmen/:id/request', component: CustomerRequestComponent, canActivate: [customerGuard] },
  { path: 'customer/notifications', component: CustomerNotificationsComponent, canActivate: [customerGuard] },
  { path: 'customer/requests', component: CustomerRequestsComponent, canActivate: [customerGuard] },
  { path: 'customer/profile', component: CustomerProfileComponent, canActivate: [customerGuard] },
  { path: 'crafts', redirectTo: '/home', pathMatch: 'full' },
  { path: '**', redirectTo: '/' },
];

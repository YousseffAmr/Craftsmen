import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs/operators';
import { CustomerService, CustomerRequest } from './customer.service';

@Component({ selector: 'app-customer-requests', standalone: true, imports: [CommonModule, RouterLink], styleUrl: './customer-requests.component.css', templateUrl: './customer-requests.component.html' })
export class CustomerRequestsComponent implements OnInit {
  requests = signal<CustomerRequest[]>([]); loading = signal(false); actionId = signal<number | null>(null); errorMessage = signal<string | null>(null);
  constructor(private readonly service: CustomerService) {}
  ngOnInit(): void { this.load(); }
  load(): void { this.loading.set(true); this.errorMessage.set(null); this.service.getRequests().pipe(finalize(() => this.loading.set(false))).subscribe({ next: value => this.requests.set(value), error: (error: Error) => { this.requests.set([]); this.errorMessage.set(error.message); } }); }
  withdraw(request: CustomerRequest): void { this.actionId.set(request.id); this.service.withdraw(request.id).pipe(finalize(() => this.actionId.set(null))).subscribe({ next: updated => this.requests.update(items => items.map(item => item.id === updated.id ? updated : item)), error: (error: Error) => this.errorMessage.set(error.message) }); }
}

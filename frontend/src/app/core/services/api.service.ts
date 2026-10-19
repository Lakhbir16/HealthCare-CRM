import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  ApiResponse,
  PagedResult,
  Patient,
  Appointment,
  VitalSign,
  ClinicalNote,
  Diagnosis,
  Allergy,
  MedicalHistory,
  Prescription,
  LabOrder,
  LabResult,
  Invoice,
  Payment,
  Doctor,
  DashboardStats,
  UserManagement,
  DevMailMessage
} from '../models/app.models';

@Injectable({
  providedIn: 'root'
})
export class ApiService {
  private readonly baseUrl = 'http://localhost:5000/api';

  constructor(private http: HttpClient) {}

  // Dashboard
  getDashboardStats(): Observable<ApiResponse<DashboardStats>> {
    return this.http.get<ApiResponse<DashboardStats>>(`${this.baseUrl}/dashboard/stats`);
  }

  // Patients
  getPatients(search?: string, page = 1, pageSize = 10): Observable<ApiResponse<PagedResult<Patient>>> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (search) params = params.set('search', search);
    return this.http.get<ApiResponse<PagedResult<Patient>>>(`${this.baseUrl}/patients`, { params });
  }

  getPatientById(id: string): Observable<ApiResponse<Patient>> {
    return this.http.get<ApiResponse<Patient>>(`${this.baseUrl}/patients/${id}`);
  }

  getMyPatientProfile(): Observable<ApiResponse<Patient>> {
    return this.http.get<ApiResponse<Patient>>(`${this.baseUrl}/patients/me`);
  }

  createPatient(data: any): Observable<ApiResponse<Patient>> {
    return this.http.post<ApiResponse<Patient>>(`${this.baseUrl}/patients`, data);
  }

  updatePatient(id: string, data: any): Observable<ApiResponse<Patient>> {
    return this.http.put<ApiResponse<Patient>>(`${this.baseUrl}/patients/${id}`, data);
  }

  getPatientHistory(patientId: string): Observable<ApiResponse<MedicalHistory[]>> {
    return this.http.get<ApiResponse<MedicalHistory[]>>(`${this.baseUrl}/patients/${patientId}/history`);
  }

  getPatientAllergies(patientId: string): Observable<ApiResponse<Allergy[]>> {
    return this.http.get<ApiResponse<Allergy[]>>(`${this.baseUrl}/patients/${patientId}/allergies`);
  }

  getPatientDiagnoses(patientId: string): Observable<ApiResponse<Diagnosis[]>> {
    return this.http.get<ApiResponse<Diagnosis[]>>(`${this.baseUrl}/patients/${patientId}/diagnoses`);
  }

  getPatientVitals(patientId: string): Observable<ApiResponse<VitalSign[]>> {
    return this.http.get<ApiResponse<VitalSign[]>>(`${this.baseUrl}/patients/${patientId}/vitals`);
  }

  getPatientClinicalNotes(patientId: string): Observable<ApiResponse<ClinicalNote[]>> {
    return this.http.get<ApiResponse<ClinicalNote[]>>(`${this.baseUrl}/patients/${patientId}/clinical-notes`);
  }

  // Appointments
  getAppointments(patientId?: string, doctorId?: string, date?: string, page = 1, pageSize = 10): Observable<ApiResponse<PagedResult<Appointment>>> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (patientId) params = params.set('patientId', patientId);
    if (doctorId) params = params.set('doctorId', doctorId);
    if (date) params = params.set('date', date);
    return this.http.get<ApiResponse<PagedResult<Appointment>>>(`${this.baseUrl}/appointments`, { params });
  }

  getAppointmentById(id: string): Observable<ApiResponse<Appointment>> {
    return this.http.get<ApiResponse<Appointment>>(`${this.baseUrl}/appointments/${id}`);
  }

  createAppointment(data: any): Observable<ApiResponse<Appointment>> {
    return this.http.post<ApiResponse<Appointment>>(`${this.baseUrl}/appointments`, data);
  }

  updateAppointmentStatus(id: string, status: string, cancellationReason?: string): Observable<ApiResponse<Appointment>> {
    let params = new HttpParams().set('status', status);
    if (cancellationReason) params = params.set('cancellationReason', cancellationReason);
    return this.http.patch<ApiResponse<Appointment>>(`${this.baseUrl}/appointments/${id}/status`, {}, { params });
  }

  rescheduleAppointment(id: string, newTimeUtc: string): Observable<ApiResponse<Appointment>> {
    const params = new HttpParams().set('newTimeUtc', newTimeUtc);
    return this.http.put<ApiResponse<Appointment>>(`${this.baseUrl}/appointments/${id}/reschedule`, {}, { params });
  }

  getDoctors(departmentId?: string, specialtyId?: string): Observable<ApiResponse<Doctor[]>> {
    let params = new HttpParams();
    if (departmentId) params = params.set('departmentId', departmentId);
    if (specialtyId) params = params.set('specialtyId', specialtyId);
    return this.http.get<ApiResponse<Doctor[]>>(`${this.baseUrl}/appointments/doctors`, { params });
  }

  // Clinical
  recordVitals(patientId: string, vitals: VitalSign): Observable<ApiResponse<VitalSign>> {
    return this.http.post<ApiResponse<VitalSign>>(`${this.baseUrl}/clinical/vitals/${patientId}`, vitals);
  }

  addClinicalNote(patientId: string, note: Partial<ClinicalNote>, doctorId: string): Observable<ApiResponse<ClinicalNote>> {
    const params = new HttpParams().set('doctorId', doctorId);
    return this.http.post<ApiResponse<ClinicalNote>>(`${this.baseUrl}/clinical/notes/${patientId}`, note, { params });
  }

  addDiagnosis(patientId: string, diagnosis: Partial<Diagnosis>, doctorId?: string): Observable<ApiResponse<Diagnosis>> {
    let params = new HttpParams();
    if (doctorId) params = params.set('doctorId', doctorId);
    return this.http.post<ApiResponse<Diagnosis>>(`${this.baseUrl}/clinical/diagnoses/${patientId}`, diagnosis, { params });
  }

  createPrescription(data: any, doctorId: string): Observable<ApiResponse<Prescription>> {
    const params = new HttpParams().set('doctorId', doctorId);
    return this.http.post<ApiResponse<Prescription>>(`${this.baseUrl}/clinical/prescriptions`, data, { params });
  }

  getPrescriptions(patientId?: string, doctorId?: string): Observable<ApiResponse<Prescription[]>> {
    let params = new HttpParams();
    if (patientId) params = params.set('patientId', patientId);
    if (doctorId) params = params.set('doctorId', doctorId);
    return this.http.get<ApiResponse<Prescription[]>>(`${this.baseUrl}/clinical/prescriptions`, { params });
  }

  updatePrescriptionStatus(id: string, status: string): Observable<ApiResponse<Prescription>> {
    const params = new HttpParams().set('status', status);
    return this.http.patch<ApiResponse<Prescription>>(`${this.baseUrl}/clinical/prescriptions/${id}/status`, {}, { params });
  }

  // Laboratory
  createLabOrder(data: any, doctorId: string): Observable<ApiResponse<LabOrder>> {
    const params = new HttpParams().set('doctorId', doctorId);
    return this.http.post<ApiResponse<LabOrder>>(`${this.baseUrl}/laboratory/orders`, data, { params });
  }

  getLabOrders(patientId?: string, doctorId?: string, status?: string): Observable<ApiResponse<LabOrder[]>> {
    let params = new HttpParams();
    if (patientId) params = params.set('patientId', patientId);
    if (doctorId) params = params.set('doctorId', doctorId);
    if (status) params = params.set('status', status);
    return this.http.get<ApiResponse<LabOrder[]>>(`${this.baseUrl}/laboratory/orders`, { params });
  }

  recordLabResult(itemId: string, data: any): Observable<ApiResponse<LabResult>> {
    return this.http.post<ApiResponse<LabResult>>(`${this.baseUrl}/laboratory/items/${itemId}/results`, data);
  }

  updateLabOrderStatus(id: string, status: string): Observable<ApiResponse<LabOrder>> {
    const params = new HttpParams().set('status', status);
    return this.http.patch<ApiResponse<LabOrder>>(`${this.baseUrl}/laboratory/orders/${id}/status`, {}, { params });
  }

  // Billing
  getInvoices(patientId?: string, status?: string, page = 1, pageSize = 10): Observable<ApiResponse<PagedResult<Invoice>>> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (patientId) params = params.set('patientId', patientId);
    if (status) params = params.set('status', status);
    return this.http.get<ApiResponse<PagedResult<Invoice>>>(`${this.baseUrl}/billing/invoices`, { params });
  }

  getInvoiceById(id: string): Observable<ApiResponse<Invoice>> {
    return this.http.get<ApiResponse<Invoice>>(`${this.baseUrl}/billing/invoices/${id}`);
  }

  createInvoice(data: any): Observable<ApiResponse<Invoice>> {
    return this.http.post<ApiResponse<Invoice>>(`${this.baseUrl}/billing/invoices`, data);
  }

  recordPayment(data: { invoiceId: string; amount: number; paymentMethod?: string; transactionReference?: string; notes?: string }): Observable<ApiResponse<Payment>> {
    return this.http.post<ApiResponse<Payment>>(`${this.baseUrl}/billing/payments`, data);
  }

  getPayments(invoiceId?: string, patientId?: string): Observable<ApiResponse<Payment[]>> {
    let params = new HttpParams();
    if (invoiceId) params = params.set('invoiceId', invoiceId);
    if (patientId) params = params.set('patientId', patientId);
    return this.http.get<ApiResponse<Payment[]>>(`${this.baseUrl}/billing/payments`, { params });
  }

  // Users Management
  getUsers(): Observable<ApiResponse<UserManagement[]>> {
    return this.http.get<ApiResponse<UserManagement[]>>(`${this.baseUrl}/users`);
  }

  createUser(data: any): Observable<ApiResponse<UserManagement>> {
    return this.http.post<ApiResponse<UserManagement>>(`${this.baseUrl}/users`, data);
  }

  updateUserRoles(userId: string, roles: string[]): Observable<ApiResponse<boolean>> {
    return this.http.put<ApiResponse<boolean>>(`${this.baseUrl}/users/${userId}/roles`, { roles });
  }

  setUserStatus(userId: string, isActive: boolean, isLockedOut: boolean): Observable<ApiResponse<boolean>> {
    const params = new HttpParams().set('isActive', isActive).set('isLockedOut', isLockedOut);
    return this.http.patch<ApiResponse<boolean>>(`${this.baseUrl}/users/${userId}/status`, {}, { params });
  }

  // Audit Logs
  getAuditLogs(page = 1, pageSize = 20): Observable<ApiResponse<PagedResult<any>>> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return this.http.get<ApiResponse<PagedResult<any>>>(`${this.baseUrl}/audit/logs`, { params });
  }

  // Development Mailbox
  getDevMailboxMessages(recipient?: string): Observable<ApiResponse<DevMailMessage[]>> {
    let params = new HttpParams();
    if (recipient) params = params.set('recipient', recipient);
    return this.http.get<ApiResponse<DevMailMessage[]>>(`${this.baseUrl}/dev-mailbox/messages`, { params });
  }
}

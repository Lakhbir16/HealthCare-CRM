export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data: T;
  errors?: string[];
}

export interface PagedResult<T> {
  items: T[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

export interface UserProfile {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  fullName: string;
  phoneNumber?: string;
  avatarUrl?: string;
  dateOfBirth?: string;
  gender?: string;
  address?: string;
  city?: string;
  state?: string;
  zipCode?: string;
  timeZone?: string;
  bio?: string;
  roles: string[];
  patientId?: string;
  doctorId?: string;
  nurseId?: string;
}

export interface LoginResponse {
  accessToken: string;
  refreshToken: string;
  expiresAtUtc: string;
  user: UserProfile;
  roles: string[];
  activeRole: string;
}

export interface ForgotPasswordResponse {
  message: string;
  expiresInSeconds: number;
  expiresAtUtc: string;
}

export interface VerifyOtpResponse {
  isValid: boolean;
  message: string;
  resetToken?: string;
  resetTokenExpiresAtUtc?: string;
}

export interface DevMailMessage {
  id: string;
  recipientEmail: string;
  subject: string;
  content: string;
  otpCode: string;
  sentAtUtc: string;
  expiresAtUtc: string;
  isExpired: boolean;
  deliveryStatus: string;
}

export interface Patient {
  id: string;
  userId: string;
  medicalRecordNumber: string;
  firstName: string;
  lastName: string;
  fullName: string;
  email: string;
  phoneNumber?: string;
  dateOfBirth?: string;
  gender?: string;
  bloodGroup?: string;
  maritalStatus?: string;
  address?: string;
  city?: string;
  state?: string;
  zipCode?: string;
  primaryCareDoctorId?: string;
  primaryCareDoctorName?: string;
  createdAtUtc: string;
}

export interface Appointment {
  id: string;
  patientId: string;
  patientName: string;
  patientMrn: string;
  doctorId: string;
  doctorName: string;
  specialty: string;
  departmentId?: string;
  departmentName?: string;
  clinicLocationId?: string;
  clinicLocationName?: string;
  scheduledDateTimeUtc: string;
  durationMinutes: number;
  appointmentType: string;
  status: string;
  reasonForVisit: string;
  cancellationReason?: string;
  notes?: string;
  checkedInAtUtc?: string;
  completedAtUtc?: string;
}

export interface VitalSign {
  id?: string;
  patientId: string;
  recordedByUserName?: string;
  recordedAtUtc?: string;
  bloodPressureSystolic?: number;
  bloodPressureDiastolic?: number;
  heartRateBpm?: number;
  temperatureFahrenheit?: number;
  respiratoryRateBpm?: number;
  oxygenSaturationPercent?: number;
  heightInches?: number;
  weightPounds?: number;
  bmi?: number;
  notes?: string;
}

export interface ClinicalNote {
  id: string;
  patientId: string;
  doctorId: string;
  doctorName: string;
  appointmentId?: string;
  noteType: string;
  subjective?: string;
  objective?: string;
  assessment?: string;
  plan?: string;
  createdAtUtc: string;
}

export interface Diagnosis {
  id: string;
  patientId: string;
  doctorId?: string;
  doctorName?: string;
  appointmentId?: string;
  icd10Code: string;
  description: string;
  status: string;
  diagnosedAtUtc: string;
  notes?: string;
}

export interface Allergy {
  id: string;
  patientId: string;
  allergen: string;
  severity: string;
  reaction?: string;
  diagnosedDate?: string;
  recordedAtUtc: string;
}

export interface MedicalHistory {
  id: string;
  patientId: string;
  condition: string;
  diagnosedDate?: string;
  status: string;
  notes?: string;
}

export interface Prescription {
  id: string;
  patientId: string;
  patientName: string;
  doctorId: string;
  doctorName: string;
  appointmentId?: string;
  prescriptionNumber: string;
  prescribedAtUtc: string;
  expirationDateUtc?: string;
  status: string;
  pharmacyNotes?: string;
  items: PrescriptionItem[];
}

export interface PrescriptionItem {
  id?: string;
  prescriptionId?: string;
  medicationName: string;
  dosage: string;
  frequency: string;
  durationDays: number;
  quantity: number;
  refillsAllowed: number;
  refillsRemaining: number;
  instructions?: string;
}

export interface LabOrder {
  id: string;
  patientId: string;
  patientName: string;
  doctorId: string;
  doctorName: string;
  orderNumber: string;
  orderedAtUtc: string;
  priority: string;
  status: string;
  clinicalIndication?: string;
  items: LabOrderItem[];
}

export interface LabOrderItem {
  id: string;
  labOrderId: string;
  testName: string;
  testCode: string;
  specimenType: string;
  status: string;
  results: LabResult[];
}

export interface LabResult {
  id: string;
  labOrderItemId: string;
  performedAtUtc: string;
  parameterName: string;
  value: string;
  unit?: string;
  referenceRange?: string;
  isAbnormal: boolean;
  flag?: string;
  interpretationNotes?: string;
}

export interface Invoice {
  id: string;
  patientId: string;
  patientName: string;
  appointmentId?: string;
  invoiceNumber: string;
  issueDateUtc: string;
  dueDateUtc: string;
  totalAmount: number;
  insuranceCoveredAmount: number;
  patientAmountDue: number;
  paidAmount: number;
  status: string;
  notes?: string;
  items: InvoiceItem[];
  payments: Payment[];
}

export interface InvoiceItem {
  id?: string;
  serviceDescription: string;
  serviceCode?: string;
  quantity: number;
  unitPrice: number;
  totalPrice: number;
}

export interface Payment {
  id: string;
  invoiceId: string;
  patientId: string;
  amount: number;
  paymentDateUtc: string;
  paymentMethod: string;
  transactionReference?: string;
  status: string;
}

export interface Doctor {
  id: string;
  userId: string;
  fullName: string;
  email: string;
  licenseNumber: string;
  npiNumber: string;
  departmentId: string;
  departmentName: string;
  specialtyId: string;
  specialtyName: string;
  consultationFee: number;
  isAcceptingPatients: boolean;
}

export interface DashboardStats {
  role: string;
  totalPatients: number;
  todayAppointments: number;
  pendingAppointments: number;
  totalDoctors: number;
  totalStaff: number;
  totalRevenue: number;
  pendingInvoicesAmount: number;
  pendingLabOrders: number;
  activePrescriptions: number;
  recentAppointments: Appointment[];
  recentVitals: VitalSign[];
  recentPrescriptions: Prescription[];
  recentLabOrders: LabOrder[];
  recentInvoices: Invoice[];
}

export interface UserManagement {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  phoneNumber?: string;
  isActive: boolean;
  isLockedOut: boolean;
  roles: string[];
  createdAtUtc: string;
}

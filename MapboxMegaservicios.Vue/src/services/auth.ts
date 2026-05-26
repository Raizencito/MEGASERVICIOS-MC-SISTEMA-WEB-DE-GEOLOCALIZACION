import api from './api'
import type { Empleado } from '@/types'

export interface LoginRequest {
  usuario: string
  password: string
}

export interface AuthResponse {
  success: boolean
  message: string
  token: string
  empleado: Empleado
}

class AuthService {
  async login(credentials: LoginRequest): Promise<boolean> {
    try {
      const response = await api.post<AuthResponse>('/auth/login', credentials)

      if (response.data.success) {
        localStorage.setItem('token', response.data.token)
        localStorage.setItem('user', JSON.stringify(response.data.empleado))
        return true
      }
      return false
    } catch (error) {
      console.error('Login error:', error)
      return false
    }
  }

  logout(): void {
    localStorage.removeItem('token')
    localStorage.removeItem('user')
    window.location.href = '/login'
  }

  getToken(): string | null {
    return localStorage.getItem('token')
  }

  getUser(): Empleado | null {
    const userStr = localStorage.getItem('user')
    return userStr ? JSON.parse(userStr) : null
  }

  isAuthenticated(): boolean {
    return !!this.getToken()
  }
}

export default new AuthService()

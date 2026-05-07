import React, { createContext, useContext, useState, useCallback, useEffect } from 'react';
import { login as apiLogin, revoke } from '../api/auth';
import { saveTokens, clearTokens, isAuthenticated, getSession } from '../utils/auth';
import { registerSessionUpdateCallback } from '../api/client';
import type { LoginDto } from '../types';

interface AuthCtx {
  isLoggedIn: boolean;
  accountId: number;
  loginName: string;
  permissions: string[];
  login: (dto: LoginDto) => Promise<void>;
  logout: () => Promise<void>;
}

const AuthContext = createContext<AuthCtx | null>(null);

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const session = getSession();
  const [isLoggedIn, setIsLoggedIn] = useState(isAuthenticated());
  const [accountId, setAccountId] = useState(session.accountId);
  const [loginName, setLoginName] = useState(session.login);
  const [permissions, setPermissions] = useState<string[]>(session.permissions);

  useEffect(() => {
    registerSessionUpdateCallback((data) => {
      setAccountId(data.accountId);
      setLoginName(data.login);
      setPermissions(data.permissions);
    });
  }, []);

  const login = useCallback(async (dto: LoginDto) => {
    const data = await apiLogin(dto);
    saveTokens(data);
    setIsLoggedIn(true);
    setAccountId(data.accountId);
    setLoginName(data.login);
    setPermissions(data.permissions);
  }, []);

  const logout = useCallback(async () => {
    const { refreshToken } = { refreshToken: localStorage.getItem('sm_refresh_token') ?? '' };
    try { await revoke(refreshToken); } catch { /* ignore */ }
    clearTokens();
    setIsLoggedIn(false);
    setAccountId(0);
    setLoginName('');
    setPermissions([]);
  }, []);

  return (
    <AuthContext.Provider value={{ isLoggedIn, accountId, loginName, permissions, login, logout }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth outside AuthProvider');
  return ctx;
}

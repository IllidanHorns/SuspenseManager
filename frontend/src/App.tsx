import { Navigate, Route, Routes } from 'react-router-dom';
import { AppLayout } from './components/layout/AppLayout';
import { LoginPage } from './pages/LoginPage';
import { DashboardPage } from './pages/DashboardPage';
import { UploadPage } from './pages/UploadPage';
import { GroupingPage } from './pages/GroupingPage';
import { SavedGroupsPage } from './pages/SavedGroupsPage';
import { GroupDetailPage } from './pages/GroupDetailPage';
import { PostponedPage } from './pages/PostponedPage';
import { SuspensesPage } from './pages/SuspensesPage';
import { AuditPage } from './pages/AuditPage';
import { BackOfficeTasksPage } from './pages/BackOfficeTasksPage';
import { BackOfficeTaskDetailPage } from './pages/BackOfficeTaskDetailPage';
import { CatalogPage } from './pages/CatalogPage';
import { AdminPage } from './pages/AdminPage';
import { MonitorPage } from './pages/MonitorPage';
import { KnowledgeBasePage } from './pages/KnowledgeBasePage';
import { SettingsPage } from './pages/SettingsPage';
import { useAuth } from './hooks/useAuth';
import { hasAdminAccess } from './utils/adminAccess';
import {
  canAccessUpload,
  canAccessGrouping,
  canAccessSavedGroups,
  canAccessGroupDetail,
  canAccessPostponed,
  canAccessSuspenses,
  canAccessAudit,
  canAccessBackoffice,
  canAccessCatalog,
  canAccessMonitor,
} from './utils/permissions';

function PrivateRoute({ children }: { children: React.ReactNode }) {
  const { isLoggedIn } = useAuth();
  return isLoggedIn ? <>{children}</> : <Navigate to="/login" replace />;
}

function PermissionRoute({ children, check }: { children: React.ReactNode; check: (p: string[]) => boolean }) {
  const { isLoggedIn, permissions } = useAuth();
  if (!isLoggedIn) return <Navigate to="/login" replace />;
  if (!check(permissions)) return <Navigate to="/" replace />;
  return <>{children}</>;
}

function AdminRoute({ children }: { children: React.ReactNode }) {
  const { isLoggedIn, permissions } = useAuth();
  if (!isLoggedIn) return <Navigate to="/login" replace />;
  if (!hasAdminAccess(permissions)) return <Navigate to="/" replace />;
  return <>{children}</>;
}

export function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route
        element={
          <PrivateRoute>
            <AppLayout />
          </PrivateRoute>
        }
      >
        <Route index element={<DashboardPage />} />
        <Route path="upload" element={<PermissionRoute check={canAccessUpload}><UploadPage /></PermissionRoute>} />
        <Route path="grouping" element={<PermissionRoute check={canAccessGrouping}><GroupingPage /></PermissionRoute>} />
        <Route path="groups" element={<PermissionRoute check={canAccessSavedGroups}><SavedGroupsPage /></PermissionRoute>} />
        <Route path="groups/:id" element={<PermissionRoute check={canAccessGroupDetail}><GroupDetailPage /></PermissionRoute>} />
        <Route path="postponed" element={<PermissionRoute check={canAccessPostponed}><PostponedPage /></PermissionRoute>} />
        <Route path="suspenses" element={<PermissionRoute check={canAccessSuspenses}><SuspensesPage /></PermissionRoute>} />
        <Route path="audit" element={<PermissionRoute check={canAccessAudit}><AuditPage /></PermissionRoute>} />
        <Route path="backoffice/tasks" element={<PermissionRoute check={canAccessBackoffice}><BackOfficeTasksPage /></PermissionRoute>} />
        <Route path="backoffice/tasks/:taskId" element={<PermissionRoute check={canAccessBackoffice}><BackOfficeTaskDetailPage /></PermissionRoute>} />
        <Route path="catalog" element={<PermissionRoute check={canAccessCatalog}><CatalogPage /></PermissionRoute>} />
        <Route path="monitor" element={<PermissionRoute check={canAccessMonitor}><MonitorPage /></PermissionRoute>} />
        <Route path="knowledge" element={<KnowledgeBasePage />} />
        <Route path="settings" element={<SettingsPage />} />
        <Route
          path="admin"
          element={
            <AdminRoute>
              <AdminPage />
            </AdminRoute>
          }
        />
        <Route path="*" element={<Navigate to="/" replace />} />
      </Route>
    </Routes>
  );
}

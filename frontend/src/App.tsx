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
import { useAuth } from './hooks/useAuth';

function PrivateRoute({ children }: { children: React.ReactNode }) {
  const { isLoggedIn } = useAuth();
  return isLoggedIn ? <>{children}</> : <Navigate to="/login" replace />;
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
        <Route path="upload" element={<UploadPage />} />
        <Route path="grouping" element={<GroupingPage />} />
        <Route path="groups" element={<SavedGroupsPage />} />
        <Route path="groups/:id" element={<GroupDetailPage />} />
        <Route path="postponed" element={<PostponedPage />} />
        <Route path="suspenses" element={<SuspensesPage />} />
        <Route path="audit" element={<AuditPage />} />
        <Route path="backoffice/tasks" element={<BackOfficeTasksPage />} />
        <Route path="backoffice/tasks/:taskId" element={<BackOfficeTaskDetailPage />} />
        <Route path="catalog" element={<CatalogPage />} />
        <Route path="*" element={<Navigate to="/" replace />} />
      </Route>
    </Routes>
  );
}

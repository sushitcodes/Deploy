import { Navigate, Outlet } from "react-router-dom";
import { useAppSelector } from "../../infrastructure/store/hooks";
import { Paths } from "../../routes/paths";

interface ProtectedRouteProps {
  allowedRoles?: string[];
}

const ProtectedRoute: React.FC<ProtectedRouteProps> = ({ allowedRoles }) => {
  // App.tsx blocks rendering until isInitialized=true, so by the time
  // this component mounts, email is already the settled auth state —
  // no need to call useGetMeQuery() again here.
  const email = useAppSelector((state) => state.auth.email);
  const roles = useAppSelector((state) => state.auth.roles);

  if (!email) return <Navigate to={Paths.login} replace />;
  if (allowedRoles && !roles.some((r) => allowedRoles.includes(r))) {
    return <Navigate to={Paths.dashboard} replace />;
  }
  return <Outlet />;
};

export default ProtectedRoute;

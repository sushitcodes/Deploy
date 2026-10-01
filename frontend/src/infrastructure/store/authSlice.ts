import { createSlice, PayloadAction } from "@reduxjs/toolkit";

interface AuthState {
  email: string | null;
  roles: string[];
  isInitialized: boolean; // true once the /me check has settled (success OR failure)
}

const initialState: AuthState = {
  email: null,
  roles: [],
  isInitialized: false,
};

const authSlice = createSlice({
  name: "auth",
  initialState,
  reducers: {
    setCredentials: (
      state,
      action: PayloadAction<{ email: string; roles: string[] }>,
    ) => {
      state.email = action.payload.email;
      state.roles = action.payload.roles;
      state.isInitialized = true;
    },
    logout: (state) => {
      state.email = null;
      state.roles = [];
      state.isInitialized = true;
    },
  },
});

export const { setCredentials, logout } = authSlice.actions;
export default authSlice.reducer;

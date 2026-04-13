import { useEffect, useMemo, useState } from "react";
import { requiresBetaAccess, supabase } from "../lib/supabaseClient";

function defaultEntitlement() {
  return {
    hasAccess: !requiresBetaAccess,
    code: null,
    grantedAt: null,
  };
}

export function useAuthState() {
  const [session, setSession] = useState(null);
  const [profile, setProfile] = useState(null);
  const [entitlement, setEntitlement] = useState(defaultEntitlement());
  const [message, setMessage] = useState("");
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    let subscription = null;

    async function bootstrap() {
      if (!supabase) {
        return;
      }

      const sessionResult = await supabase.auth.getSession();
      setSession(sessionResult.data.session);

      if (sessionResult.data.session?.user) {
        await ensureProfile(sessionResult.data.session.user);
        await refreshEntitlement(sessionResult.data.session.user.id);
      }

      const authListener = supabase.auth.onAuthStateChange(async (_event, nextSession) => {
        setSession(nextSession);

        if (nextSession?.user) {
          await ensureProfile(nextSession.user);
          await refreshEntitlement(nextSession.user.id);
        } else {
          setProfile(null);
          setEntitlement(defaultEntitlement());
        }
      });

      subscription = authListener.data.subscription;
    }

    bootstrap();

    return () => {
      subscription?.unsubscribe();
    };
  }, []);

  async function ensureProfile(user) {
    if (!supabase || !user) {
      return null;
    }

    const { data: existingProfile, error: existingError } = await supabase
      .from("profiles")
      .select("id, display_name")
      .eq("id", user.id)
      .maybeSingle();

    if (existingError) {
      setMessage(existingError.message);
      return null;
    }

    if (existingProfile) {
      setProfile(existingProfile);
      return existingProfile;
    }

    const { data, error } = await supabase
      .from("profiles")
      .insert({
        id: user.id,
        email: user.email,
        display_name: user.user_metadata?.display_name || user.email?.split("@")[0] || "Player",
        updated_at: new Date().toISOString(),
      })
      .select("id, display_name")
      .single();

    if (error) {
      setMessage(error.message);
      return null;
    }

    setProfile(data);
    return data;
  }

  async function refreshEntitlement(userId) {
    if (!supabase || !requiresBetaAccess) {
      const freeEntitlement = defaultEntitlement();
      setEntitlement(freeEntitlement);
      return freeEntitlement;
    }

    const { data, error } = await supabase
      .from("beta_entitlements")
      .select("invite_code, granted_at")
      .eq("user_id", userId)
      .maybeSingle();

    if (error) {
      setMessage(error.message);
      return defaultEntitlement();
    }

    const nextEntitlement = {
      hasAccess: !!data,
      code: data?.invite_code || null,
      grantedAt: data?.granted_at || null,
    };

    setEntitlement(nextEntitlement);
    return nextEntitlement;
  }

  async function signIn(email, password) {
    if (!supabase) {
      setMessage("Supabase is not configured yet.");
      return { success: false };
    }

    setBusy(true);
    setMessage("");

    try {
      const { error } = await supabase.auth.signInWithPassword({ email, password });
      if (error) {
        setMessage(error.message);
        return { success: false };
      }

      setMessage("Welcome back.");
      return { success: true };
    } finally {
      setBusy(false);
    }
  }

  async function signUp(email, password) {
    if (!supabase) {
      setMessage("Supabase is not configured yet.");
      return { success: false };
    }

    setBusy(true);
    setMessage("");

    try {
      const { error } = await supabase.auth.signUp({ email, password });
      if (error) {
        setMessage(error.message);
        return { success: false };
      }

      setMessage("Account created. Check your inbox if email confirmation is enabled.");
      return { success: true };
    } finally {
      setBusy(false);
    }
  }

  async function signOut() {
    if (!supabase) {
      return;
    }

    setBusy(true);

    try {
      const { error } = await supabase.auth.signOut();

      if (error) {
        setMessage(error.message);
        return { success: false };
      }

      setSession(null);
      setProfile(null);
      setEntitlement(defaultEntitlement());
      setMessage("");
      return { success: true };
    } finally {
      setBusy(false);
    }
  }

  async function saveProfile(displayName) {
    if (!supabase || !session?.user) {
      return { success: false };
    }

    setBusy(true);
    setMessage("");

    try {
      const nextDisplayName = displayName || "Player";
      const { data, error } = await supabase
        .from("profiles")
        .upsert({
          id: session.user.id,
          email: session.user.email,
          display_name: nextDisplayName,
          updated_at: new Date().toISOString(),
        })
        .select("id, display_name")
        .single();

      if (error) {
        setMessage(error.message);
        return { success: false };
      }

      const authUpdate = await supabase.auth.updateUser({
        data: {
          display_name: nextDisplayName,
        },
      });

      if (authUpdate.error) {
        setMessage(authUpdate.error.message);
        return { success: false };
      }

      setProfile(data);
      setMessage("Profile updated.");
      return { success: true };
    } finally {
      setBusy(false);
    }
  }

  async function redeemInviteCode(code) {
    if (!supabase) {
      setMessage("Supabase is not configured yet.");
      return { success: false };
    }

    if (!code) {
      setMessage("Enter a beta key first.");
      return { success: false };
    }

    setBusy(true);
    setMessage("");

    try {
      const { data, error } = await supabase.rpc("redeem_invite_code", {
        input_code: code,
      });

      if (error) {
        setMessage(error.message);
        return { success: false };
      }

      const result = Array.isArray(data) ? data[0] : data;
      if (!result?.success) {
        setMessage(result?.message || "That beta key could not be redeemed.");
        return { success: false };
      }

      await refreshEntitlement(session.user.id);
      setMessage(result.message || "Beta access granted.");
      return { success: true };
    } finally {
      setBusy(false);
    }
  }

  return useMemo(
    () => ({
      session,
      profile,
      entitlement,
      message,
      busy,
      hasBetaAccess: !!entitlement.hasAccess,
      signIn,
      signUp,
      signOut,
      saveProfile,
      redeemInviteCode,
    }),
    [session, profile, entitlement, message, busy]
  );
}

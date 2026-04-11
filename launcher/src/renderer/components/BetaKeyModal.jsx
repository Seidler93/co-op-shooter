import { useEffect, useState } from "react";

export default function BetaKeyModal({ isOpen, busy, errorMessage, onClose, onRedeem }) {
  const [code, setCode] = useState("");

  useEffect(() => {
    if (!isOpen) {
      setCode("");
    }
  }, [isOpen]);

  if (!isOpen) {
    return null;
  }

  return (
    <div className="modal-backdrop" role="presentation" onClick={onClose}>
      <div
        className="modal-card panel"
        role="dialog"
        aria-modal="true"
        aria-labelledby="beta-modal-title"
        onClick={(event) => event.stopPropagation()}
      >
        <p className="eyebrow">Beta Access</p>
        <h3 id="beta-modal-title">Enter your beta key</h3>
        <p className="muted">
          This build is invite-only. Redeem a valid key to unlock download and install access for this account.
        </p>

        <label className="field">
          <span>Beta Key</span>
          <input
            value={code}
            onChange={(event) => setCode(event.target.value.toUpperCase())}
            placeholder="BETA-XXXX"
          />
        </label>

        {errorMessage ? <div className="notice warning">{errorMessage}</div> : null}

        <div className="button-row">
          <button className="primary" disabled={busy || !code.trim()} onClick={() => onRedeem(code.trim())}>
            Unlock Access
          </button>
          <button className="secondary" disabled={busy} onClick={onClose}>
            Cancel
          </button>
        </div>
      </div>
    </div>
  );
}

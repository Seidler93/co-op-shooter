export default function ConfirmModal({
  isOpen,
  title,
  eyebrow = "Confirm Action",
  message,
  confirmLabel = "Confirm",
  cancelLabel = "Cancel",
  danger = false,
  busy = false,
  onConfirm,
  onClose,
}) {
  if (!isOpen) {
    return null;
  }

  return (
    <div className="modal-backdrop" role="presentation" onClick={onClose}>
      <div
        className="modal-card panel"
        role="dialog"
        aria-modal="true"
        aria-labelledby="confirm-modal-title"
        onClick={(event) => event.stopPropagation()}
      >
        <p className="eyebrow">{eyebrow}</p>
        <h3 id="confirm-modal-title">{title}</h3>
        <p className="muted">{message}</p>

        <div className="button-row">
          <button className={danger ? "primary danger" : "primary"} disabled={busy} onClick={onConfirm}>
            {confirmLabel}
          </button>
          <button className="secondary" disabled={busy} onClick={onClose}>
            {cancelLabel}
          </button>
        </div>
      </div>
    </div>
  );
}

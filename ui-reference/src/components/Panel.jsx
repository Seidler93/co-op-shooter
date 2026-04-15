export function Panel({ className = "", children }) {
  return <div className={`ui-panel ${className}`.trim()}>{children}</div>;
}

export function PanelHeading({ kicker, title, status }) {
  return (
    <div className="panel-heading">
      <div>
        {kicker ? <p className="panel-kicker">{kicker}</p> : null}
        <h3>{title}</h3>
      </div>
      {status}
    </div>
  );
}

export function StatusPill({ tone, children }) {
  return <span className={`status-pill ${tone}`.trim()}>{children}</span>;
}

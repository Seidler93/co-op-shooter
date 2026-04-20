export default function ScreenFrame({ backdropClassName, gridClassName, topBar, children }) {
  return (
    <div className="screen-frame">
      <div className={`screen-backdrop ${backdropClassName}`.trim()}></div>
      <div className={`screen-frame-shell ${topBar ? "has-topbar" : ""}`.trim()}>
        {topBar ? <div className="screen-topbar">{topBar}</div> : null}
        <div className={`screen-grid ${gridClassName}`.trim()}>{children}</div>
      </div>
    </div>
  );
}

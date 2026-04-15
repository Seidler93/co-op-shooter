export default function ScreenFrame({ backdropClassName, gridClassName, children }) {
  return (
    <div className="screen-frame">
      <div className={`screen-backdrop ${backdropClassName}`.trim()}></div>
      <div className={`screen-grid ${gridClassName}`.trim()}>{children}</div>
    </div>
  );
}

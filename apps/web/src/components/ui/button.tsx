import type { ButtonHTMLAttributes, ReactNode } from "react";

type ButtonVariant = "primary" | "secondary" | "quiet" | "danger";
type ButtonSize = "small" | "medium";

export type ButtonProps = ButtonHTMLAttributes<HTMLButtonElement> & Readonly<{
  variant?: ButtonVariant;
  size?: ButtonSize;
  leadingIcon?: ReactNode;
}>;

export function Button({
  variant = "primary",
  size = "medium",
  leadingIcon,
  className = "",
  children,
  type = "button",
  ...props
}: ButtonProps) {
  return (
    <button
      className={`button button-${variant} button-${size} ${className}`.trim()}
      type={type}
      {...props}
    >
      {leadingIcon ? <span className="button-icon" aria-hidden="true">{leadingIcon}</span> : null}
      <span>{children}</span>
    </button>
  );
}

export type IconButtonProps = ButtonHTMLAttributes<HTMLButtonElement> & Readonly<{
  label: string;
  children: ReactNode;
}>;

export function IconButton({ label, children, className = "", type = "button", ...props }: IconButtonProps) {
  return (
    <button className={`icon-button ${className}`.trim()} type={type} aria-label={label} {...props}>
      {children}
    </button>
  );
}

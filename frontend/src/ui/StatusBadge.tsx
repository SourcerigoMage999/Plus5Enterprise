type StatusBadgeProps = {
  readonly label: string
  readonly tone: 'positive' | 'warning' | 'neutral'
  readonly className?: string
}

/** Presentation only: callers own domain labels; a static badge is not a live announcement. */
export function StatusBadge({ label, tone, className }: StatusBadgeProps) {
  return <span className={className ?? 'ds-status'} data-tone={tone}>{label}</span>
}

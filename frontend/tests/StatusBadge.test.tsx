import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { StatusBadge } from '../src/ui/StatusBadge.tsx'

describe('StatusBadge presentation boundary', () => {
  it('preserves domain-specific labels without inferring readiness or announcing static data', () => {
    render(<><StatusBadge label="Aktivan" tone="positive" /><StatusBadge label="Aktivna" tone="positive" /><StatusBadge label="Na čekanju" tone="warning" /></>)
    expect(screen.getByText('Aktivan')).toBeVisible()
    expect(screen.getByText('Aktivna')).toBeVisible()
    expect(screen.getByText('Na čekanju')).toBeVisible()
    expect(screen.queryByText('Spreman')).not.toBeInTheDocument()
    expect(screen.queryByRole('status')).not.toBeInTheDocument()
  })
})

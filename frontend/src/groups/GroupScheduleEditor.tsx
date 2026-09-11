import { useState } from 'react'
import { useGroupResource, type Page } from './groupsApi.ts'

import { emptySchedule, dayNames, type ScheduleSlot, type GroupSchedule } from './groupCreationModels.ts'

export function GroupScheduleEditor({ value, onChange }: { readonly value: GroupSchedule; readonly onChange: (value: GroupSchedule) => void }) {
  function slotChange(key: string, change: Partial<ScheduleSlot>) { onChange({ ...value, slots: value.slots.map(slot => slot.key === key ? { ...slot, ...change } : slot) }) }
  return <>
    <p className="group-create-muted">Raspored je opcionalan. Vrijeme: Europe/Zagreb. Nakon spremanja priprema se početnih 12 tjedana termina, bez ograničavanja trajanja grupe.</p>
    {value.slots.map((slot, index) => <fieldset className="group-schedule-slot" key={slot.key}><legend>{index + 1}. termin</legend>
      <label><span className="sr-only">Dan {index + 1}. termina</span><select value={slot.dayOfWeek} onChange={event => slotChange(slot.key, { dayOfWeek: Number(event.target.value) })}>{[1, 2, 3, 4, 5, 6, 0].map(day => <option key={day} value={day}>{dayNames[day]}</option>)}</select></label>
      <label><span className="sr-only">Početak {index + 1}. termina</span><input type="time" required value={slot.start} onChange={event => slotChange(slot.key, { start: event.target.value })} /></label>
      <span aria-hidden="true">–</span>
      <label><span className="sr-only">Završetak {index + 1}. termina</span><input type="time" required value={slot.end} onChange={event => slotChange(slot.key, { end: event.target.value })} /></label>
      <button type="button" aria-label={`Ukloni ${index + 1}. termin`} onClick={() => { const slots = value.slots.filter(s => s.key !== slot.key); onChange(slots.length ? { ...value, slots } : emptySchedule) }}>×</button>
    </fieldset>)}
    <button className="group-schedule-add" type="button" disabled={value.slots.length >= 14} onClick={() => onChange({ ...value, slots: [...value.slots, { key: crypto.randomUUID(), dayOfWeek: 1, start: '', end: '' }] })}>+ Dodaj termin</button>
    {value.slots.length > 0 && <>
      <div className="group-schedule-dates"><label className="group-create-field"><span>Datum početka *</span><input type="date" required value={value.startsOn} onChange={event => onChange({ ...value, startsOn: event.target.value })} /></label><label className="group-create-field"><span>Datum završetka (opcionalno)</span><input type="date" min={value.startsOn} value={value.endsOn} onChange={event => onChange({ ...value, endsOn: event.target.value })} /></label></div>
      <small>Ostavite završetak prazan za grupu bez određenog kraja.</small>
    </>}
  </>
}

export function GroupLocationPicker({ value, onChange }: { readonly value: GroupSchedule; readonly onChange: (value: GroupSchedule) => void }) {
  const [search, setSearch] = useState('')
  const [page, setPage] = useState(1)
  const [revision, setRevision] = useState(0)
  const locations = useGroupResource<Page<{ id: string; name: string }>>(`/groups/create-locations?page=${page}&search=${encodeURIComponent(search)}`, revision)
  return <>
      <label className="group-create-field"><span>Pretraži lokacije</span><input type="search" maxLength={100} value={search} onChange={event => { setSearch(event.target.value); setPage(1) }} /></label>
      {locations?.error ? <p role="alert">Lokacije nije moguće učitati. <button type="button" onClick={() => setRevision(v => v + 1)}>Ponovi lokacije</button></p> : !locations?.data ? <p role="status">Učitavanje lokacija…</p> : <>
        <label className="group-create-field"><span>Lokacija / učionica</span><select value={value.locationId} onChange={event => onChange({ ...value, locationId: event.target.value, locationName: locations.data!.items.find(l => l.id === event.target.value)?.name ?? '' })}><option value="">Bez lokacije</option>{value.locationId && !locations.data.items.some(l => l.id === value.locationId) && <option value={value.locationId}>{value.locationName}</option>}{locations.data.items.map(l => <option key={l.id} value={l.id}>{l.name}</option>)}</select></label>
        {locations.data.totalCount === 0 && <p>Nema lokacija za ovu pretragu.</p>}
        {locations.data.totalPages > 1 && <nav className="group-create-paging" aria-label="Stranice lokacija"><button type="button" disabled={page <= 1} onClick={() => setPage(page - 1)}>Prethodne lokacije</button><span>{page} / {locations.data.totalPages}</span><button type="button" disabled={page >= locations.data.totalPages} onClick={() => setPage(page + 1)}>Sljedeće lokacije</button></nav>}
      </>}
  </>
}

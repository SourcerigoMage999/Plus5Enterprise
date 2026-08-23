import { readPublicEnvironment } from './publicEnvironment.ts'

export const environment = readPublicEnvironment(import.meta.env)

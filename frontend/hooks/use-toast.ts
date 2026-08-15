"use client"

import * as React from "react"

const TOAST_LIMIT = 1
const TOAST_AUTO_DISMISS = 5000

type ToasterToast = {
  id: string
  title?: string
  description?: string
  action?: React.ReactNode
  variant?: "default" | "destructive"
}

type Action =
  | { type: "ADD_TOAST"; toast: ToasterToast }
  | { type: "DISMISS_TOAST"; toastId: string }
  | { type: "REMOVE_TOAST"; toastId: string }

interface State { toasts: ToasterToast[] }

const toastTimeouts = new Map<string, ReturnType<typeof setTimeout>>()

let count = 0
function genId() {
  count = (count + 1) % Number.MAX_SAFE_INTEGER
  return count.toString()
}

function clearToastTimeout(toastId: string) {
  const t = toastTimeouts.get(toastId)
  if (t) {
    clearTimeout(t)
    toastTimeouts.delete(toastId)
  }
}

const reducer = (state: State, action: Action): State => {
  switch (action.type) {
    case "ADD_TOAST":
      return { ...state, toasts: [action.toast, ...state.toasts].slice(0, TOAST_LIMIT) }
    case "DISMISS_TOAST":
      // The close button (and the auto-dismiss timer) both land here. Remove the
      // toast immediately so the X button is responsive (no 5s ghost delay).
      clearToastTimeout(action.toastId)
      return { ...state, toasts: state.toasts.filter((t) => t.id !== action.toastId) }
    case "REMOVE_TOAST":
      return { ...state, toasts: state.toasts.filter((t) => t.id !== action.toastId) }
  }
}

const listeners: Array<(state: State) => void> = []
let memoryState: State = { toasts: [] }

function dispatch(action: Action) {
  memoryState = reducer(memoryState, action)
  listeners.forEach((listener) => listener(memoryState))
}

type Toast = Omit<ToasterToast, "id">

function toast({ ...props }: Toast) {
  const id = genId()
  dispatch({ type: "ADD_TOAST", toast: { ...props, id } })
  // Auto-dismiss after a few seconds so a toast never lingers forever.
  const timeout = setTimeout(() => {
    toastTimeouts.delete(id)
    dispatch({ type: "DISMISS_TOAST", toastId: id })
  }, TOAST_AUTO_DISMISS)
  toastTimeouts.set(id, timeout)
  return {
    id,
    dismiss: () => dispatch({ type: "DISMISS_TOAST", toastId: id }),
  }
}

function useToast() {
  const [state, setState] = React.useState<State>(memoryState)

  React.useEffect(() => {
    listeners.push(setState)
    return () => {
      const index = listeners.indexOf(setState)
      if (index > -1) listeners.splice(index, 1)
    }
  }, [])

  return {
    ...state,
    toast,
    dismiss: (toastId: string) => dispatch({ type: "DISMISS_TOAST", toastId }),
  }
}

export { useToast, toast }
export type { ToasterToast }

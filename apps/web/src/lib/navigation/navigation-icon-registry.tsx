"use client";

import {
  BadgeDollarSign,
  Boxes,
  Cable,
  CalendarClock,
  CalendarRange,
  ChartNoAxesCombined,
  CircleGauge,
  CirclePercent,
  Cpu,
  Factory,
  FileWarning,
  Gauge,
  LayoutDashboard,
  ListTodo,
  MessagesSquare,
  Package,
  PackageSearch,
  Settings,
  ShieldCheck,
  Ship,
  ShoppingBasket,
  TriangleAlert,
  UsersRound,
  Wrench,
  Zap,
  type LucideIcon,
} from "lucide-react";

import type { NavigationIconKey } from "./navigation-types";

export const navigationIconRegistry = {
  "list-todo": ListTodo,
  "layout-dashboard": LayoutDashboard,
  "messages-square": MessagesSquare,
  "shopping-basket": ShoppingBasket,
  "calendar-range": CalendarRange,
  factory: Factory,
  ship: Ship,
  "calendar-clock": CalendarClock,
  cpu: Cpu,
  boxes: Boxes,
  molds: Boxes,
  package: Package,
  wrench: Wrench,
  "shield-check": ShieldCheck,
  zap: Zap,
  cable: Cable,
  "package-search": PackageSearch,
  "triangle-alert": TriangleAlert,
  "circle-percent": CirclePercent,
  gauge: Gauge,
  "file-warning": FileWarning,
  "badge-dollar-sign": BadgeDollarSign,
  "users-round": UsersRound,
  "circle-gauge": CircleGauge,
  "chart-no-axes-combined": ChartNoAxesCombined,
  settings: Settings,
} satisfies Record<NavigationIconKey, LucideIcon>;

export function resolveNavigationIcon(iconKey: NavigationIconKey): LucideIcon {
  return navigationIconRegistry[iconKey] ?? LayoutDashboard;
}

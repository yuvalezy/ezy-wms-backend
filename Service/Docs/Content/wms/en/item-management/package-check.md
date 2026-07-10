---
title: Package Check (legacy)
description: Historical package-check reference; no standalone package-check route is exposed in the current WMS frontend.
---

> **Current WMS status:** This standalone Package Check page is retained for
> reference because it exists in the source wiki, but `/packageCheck` is not a
> route in the current frontend and this page is not a current operator workflow.

Package operations currently supported by WMS are part of the Picking workflow:

- package labels can be listed or created from a picking document when
  `EnablePickingPackageLabels` is enabled;
- picking items can be assigned to labels and supervisors can use the repack flow
  when `EnablePostPickRepack` is enabled;
- the backend endpoints are under `PickingController`, not a standalone package
  check controller.

Use [Picking Process](/docs/picking/process) and [Picking Supervisor](/docs/picking/supervisor)
for the current workflow. Do not direct users to `/packageCheck`.

# MyTaskTracker microservice Helm chart

This reusable chart deploys each backend service and frontend application into the `dev` namespace. Argo CD ApplicationSet supplies a service-specific values file from `deploy/values/base/` together with shared environment values from `deploy/environments/dev/`.

## What the chart renders

| Template | Purpose |
| --- | --- |
| `deployment.yaml` | Workload Deployment, container image, environment configuration, probes, and resources. |
| `service.yaml` | ClusterIP service for in-cluster traffic. |
| `ingress.yaml` | Optional NGINX Ingress for public routes. |
| `migration-job.yaml` | Optional Argo CD Sync hook that runs EF Core migration before the workload. |
| `hpa.yaml` | Optional HorizontalPodAutoscaler when enabled by service values. |
| `secret-provider-class.yaml` | Optional Key Vault CSI integration and synced Kubernetes Secret. |
| `servicemonitor.yaml` | Optional Prometheus ServiceMonitor. |
| `backup-cronjob.yaml` | Database-backup CronJob when enabled for AuthService values. |

## Delivery path

On `main`, CI validates changed source and Helm configuration, publishes changed images to ACR, then updates the corresponding image tag in `deploy/values/base/`. Argo CD watches the Git revision and reconciles the desired state into AKS.

The CI/CD diagram illustrates this change-to-deployment path.

![CI checks, image publication, GitOps tag update, and Argo CD delivery](../../../reports/Figures/cicd-workflow.png)

The Root App and its child applications shown below illustrate the Argo CD app-of-apps and ApplicationSet structure. This is an observed UI capture, not a permanent claim that the applications remain healthy or synced; verify live state before an operational action.

![Argo CD Root App and its child platform applications](../../../reports/Figures/argocd-root-app.png)

## Typical service values

```yaml
fullnameOverride: "task-service"
image:
  repository: "backend/taskservice"
  tag: "<git-sha>"
service:
  port: 80
migration:
  enabled: true
  dllName: "Tracker.TaskService.dll"
resources:
  requests:
    cpu: 50m
    memory: 128Mi
autoscaling:
  enabled: true
```

Do not place credentials in a values file. Use the Key Vault mapping in the service values and the `SecretProviderClass` template. AuthService also receives its registered OAuth redirect URIs through Helm environment values.

## Render before changing a chart

Run from the repository root and select the service values you changed:

```powershell
helm lint deploy/charts/microservice
helm template authservice deploy/charts/microservice `
  -f deploy/values/base/authservice.yaml `
  -f deploy/environments/dev/values.yaml `
  -f deploy/environments/dev/akv-values.yaml
```

Review the rendered Deployment, migration Job, resource requests, HPA target, and ingress route together. An HPA must not be expected to work before the target Deployment exists and exposes CPU requests.

## Images used here

This README embeds `cicd-workflow.png` and `argocd-root-app.png` from `reports/Figures/` through relative Markdown paths. To replace either visual, overwrite the file in that shared folder while retaining its filename. See [the figure checklist](../../../reports/Figures/README.md) for the full image map and capture rules.

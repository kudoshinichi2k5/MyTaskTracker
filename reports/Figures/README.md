# Figure catalogue

The current report is `../LeTrungKien_Internship_Report.pdf`. Its 13 figures
use the filenames below; the same assets are embedded in the README files.

## Images currently imported by the report

| Filename | Report section | What the image should show |
| --- | --- | --- |
| `application-architecture.png` | Application architecture | CustomerApp, AdminPortal, AuthService, the four resource services, Valkey, and MariaDB databases. |
| `authentication-flow.png` | Authentication flow | The three phases A--C: Authorization Code + PKCE, opaque-token verification through `/verify`, and refresh/revocation. |
| `customer-app.png` | Application UI | A signed-in CustomerApp page showing tasks/projects; avoid showing credentials or tokens. |
| `admin-portal.png` | Application UI | A signed-in AdminPortal page showing an admin function such as user, project, or task management. |
| `azure-aks-architecture.png` | Azure and IaC architecture | Terraform, GitHub Actions, ACR, AKS, Argo CD, ingress, Key Vault, monitoring, and backup storage. |
| `cicd-workflow.png` | CI/CD and GitOps | Application change detection, tests/security checks, image push to ACR, image-tag update, and Argo CD sync. |
| `argocd-root-app.png` | Argo CD delivery | The Root App and its child platform applications/ApplicationSet, ideally with `Healthy` and `Synced` visible. |
| `monitoring-sources.png` | Monitoring architecture / component map | Roles of Prometheus, Alloy, Loki, and Grafana, and how their data sources relate; this is a component view, not a timeline. |
| `monitoring-workflow.png` | Telemetry flow / sequence | How metrics and logs are collected, stored in Prometheus, Loki, and Blob Storage, then queried in Grafana; this is the end-to-end movement view. |
| `grafana-prometheus-query.png` | Monitoring evidence | Grafana Explore or dashboard showing a Prometheus node-memory query. |
| `grafana-nginx-dashboard.png` | Monitoring evidence | NGINX Ingress request rate and HTTP status dashboard. |
| `loki-application-logs.png` | Monitoring evidence | Grafana Explore query and application logs from Loki. |
| `loki-ingress-logs.png` | Monitoring evidence | Grafana Explore query and NGINX Ingress access logs from Loki. |

## README image map

Do not copy an image into each folder: keep one canonical file here and use a
relative Markdown link.

| README | Location in README | Images embedded | Relative Markdown path from that README |
| --- | --- | --- | --- |
| Root `../../README.md` | architecture, authentication, interfaces, infrastructure, CI/CD, GitOps, and observability sections | all 13 PNG files in this folder, in the same subject order as the report | `reports/Figures/<filename>` |
| Application `../../app/README.md` | overview, authentication, and UI screenshots | `application-architecture.png`, `authentication-flow.png`, `customer-app.png`, `admin-portal.png` | `../reports/Figures/<filename>` |
| Infrastructure `../../infrastructure/README.md` | Azure and AKS architecture overview, before the Azure layers | `azure-aks-architecture.png` | `../reports/Figures/azure-aks-architecture.png` |
| Helm chart `../../deploy/charts/microservice/README.md` | `Delivery path` | `cicd-workflow.png`, `argocd-root-app.png` | `../../../reports/Figures/<filename>` |
| This README | catalogue only | no image is embedded here | not applicable |

Use one presentation rule in every README: explain the subject and give its context in text first, then place the related image below that explanation.

Use this syntax when adding a new README image:

```markdown
![Short, descriptive alt text](relative/path/to/image.png)
```

For example, from the root README:

```markdown
![OAuth Authorization Code with PKCE](reports/Figures/authentication-flow.png)
```

All 13 current PNG assets are now embedded in a README with a technical
purpose. Do not add screenshots merely as decoration; keep them in the README
whose topic they explain.

## Capture guidance

- Use PNG and crop browser chrome, terminal prompts, credentials, tokens, and unrelated personal information.
- Keep the text readable at report scale; use a high-resolution source for diagrams.
- Do not alter runtime screenshots to imply a status that was not observed. State the capture date/environment in the surrounding report text when it matters.

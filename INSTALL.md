# Installing
## Create Azure Resources
1. Create an Azure Storage (v2) account and enable "Static Website" support.
2. Create a container called "authjanitor" ("Private" security)
3. Create an Azure Function for AuthJanitor.Automation.AdminApi including proxies
4. Upload data from AuthJanitor.Automation.AdminUi (published) to "$web" container (created by enabling "Static Website")
5. Set STORAGE_WEB_URL to the web endpoint (ends in `.web.core.windows.net`)
6. Set FUNCTIONS_URL to the AuthJanitor Admin Functions published root URL
7. Add authentication with preferred IdP
8. Add approles "auditor", "serviceOperator", "secretAdmin", "resourceAdmin", "globalAdmin"
9. Assign approles to users who can use AJ
10. Set CLIENT_ID, CLIENT_SECRET, and TENANT_ID for on-behalf-of token exchange

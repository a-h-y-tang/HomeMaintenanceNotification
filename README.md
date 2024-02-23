# Home Maintenance Notification Function

This function gets the maintenance tasks that are due for completion this weekend from the HomeMaintenance API and sends those details as an email via SendGrid.

## Getting Started

Notes for how to setup and run this function:


* Run HomeMaintenance API
* TODO - setup app registrations in Azure Entra ID (currently no bearer token checks in the API or retrieval of tokens in this function)
* Create SendGrid account (use the 100 emails for free per month)
* Install Template as a Dynamic template
* Create API Key in SendGrid
* Setup Sender
* Populate *.settings.json with SendGrid details
* TODO - use the CI/CD pipeline to deploy to Azure as a function app (currently running entire solution from the local laptop)
* For adhoc runs and manual tests, setup a Postman collection to POST to /api/notifyTasks
# FIDO2 Identity Server

This applicaiton is developed for mine bachelor's thesis which was: Authentication Implementation Using FIDO2 (Serbian: Implementacija autentifikacije korišćenjem FIDO2 mehanizama). It is used to expose the most important features of FIDO2 specification. Serbian readers can check PDF document on this repo that covers all details about FIDO2 specification and applications that I developed.

## System architecture
The system consists of three parts that are representing three separate applicaitons:
  1) FIDO2 Identity Manager and Authentication Server that represents web application that can be used as identity manager and for user authentication using passwords only, 2-factor authentication or passwordless authencication using FIDO2;
  2) Test client web application that has protected resources. This application uses previously mentioned FIDO2 Identity Manager as authenticaiton server;
  3) Desktop applicaiton that can be used for signing using smart cards.

## FIDO2 Identity Manager
This application uses Identity Server's implementation of OAuth2 flow for user authentication and Identity Server's implementation of AspNetCoreIdentity for user management (for more info check https://identityserver4.readthedocs.io/en/latest/). 

OAuth2 Clients can be configured in Config.cs files. 

In order to use FIDO2 feauters of the application, user needs to register using register form on main page. After that user can login using password authenticaiton. After login, user can register new FIDO2 authenticator or Smart Card. In order to register FIDO2 authenticator, user must connect authenticator to device that is running the applicaiton (same thing applies to the smart card). 

When user wants to use Smart Card features of the application, user needs to run Smart Card Signer application (https://github.com/mladjanmih/smart-card-signer).
After user registers FIDO2 authenticator, it will be able to use it from now on for two-factor login, or for passwordless login, or for resource signing (see Test client web application).

## Test Client Web Application
As mentioned, this application uses FIDO2 Identity Manager as authentication server. URL to FIDO2 Identity Server can be configured in appsettings.json file by changing the Identity field. 

When user runs this application blank home page will be shown with Login button in header. When users clicks on login button it will be redirected to the login page of FIDO2 Identity Manager in order to perform login. When user logins successfully, it will be redirected back to the Client Application. 

Authenticated user can initiate payments. When a payment is initiatet it can be authorized. When user clicks on Authorize button next to the payment that it wants to authorize, it will be redirected to the FIDO2 Identity Manager in order to perform authorization using FIDO2 authenticator or smart card.

## More information about FIDO2 
FIDO Alliance: https://fidoalliance.org/
Web Authn: https://webauthn.guide/

Mladjan Mihajlovic 2019.

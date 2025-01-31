﻿import { JsonRequestHandlerMethodName, JsonRequestHandlerName } from './Constants.js';

export class BlazorState {

  jsonRequestHandler: any;
  reduxDevTools: any;

  async DispatchRequest(requestTypeFullName: string, request: any) {
    const requestAsJson = JSON.stringify(request);

    console.log(`Dispatching request of Type ${requestTypeFullName}: ${requestAsJson}`);
    await this.jsonRequestHandler.invokeMethodAsync(JsonRequestHandlerMethodName, requestTypeFullName, requestAsJson);
  }
}

export const blazorState = new BlazorState();

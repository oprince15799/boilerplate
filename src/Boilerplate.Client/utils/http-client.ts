import axios, { isAxiosError, AxiosError, AxiosInstance, InternalAxiosRequestConfig as AxiosRequestConfig, AxiosResponse, CreateAxiosDefaults, HttpStatusCode } from "axios";
import axiosRetry from 'axios-retry';
import { BehaviorSubject } from 'rxjs';
import QueryString from 'qs';
import PQueue from 'p-queue';

const isUnauthorizedError = (error: AxiosError | Error) => isAxiosError(error) && error.response?.status == HttpStatusCode.Unauthorized;

const isBadRequestError = (error: AxiosError | Error) => isAxiosError(error) && error.response?.status == HttpStatusCode.BadRequest;

export type User = {
  tokenType: string,
  accessToken: string,
  refreshToken: string
};

export type AuthCredentials = {
  username: string,
  password: string
};

export interface AuthActions {
  generate: (axios: AxiosInstance, provider: string | null, credentials: AuthCredentials | null) => Promise<AxiosResponse<any, any>>,
  refresh: (axios: AxiosInstance, token: string) => Promise<AxiosResponse<any, any>>,
  revoke: (axios: AxiosInstance, token: string) => Promise<AxiosResponse<any, any>>
};

export interface HttpClient extends AxiosInstance {
  user: BehaviorSubject<User>,
  signIn: (credentials: AuthCredentials) => Promise<AxiosResponse<any, any>>,
  signInWith: (provider: string) => Promise<AxiosResponse<any, any>>,
  signOut: () => Promise<AxiosResponse<any, any>>
};

export interface HttpClientOptions extends CreateAxiosDefaults {
  tokens: AuthActions,
};

export const createHttpClient = (options: HttpClientOptions): HttpClient => {
  const queue = new PQueue();

  const httpClient = axios.create(options = {
    ...options,
    paramsSerializer: {
      encode: (params) => QueryString.stringify(params, { arrayFormat: 'repeat' })
    },
    withCredentials: true
  });

  const user = new BehaviorSubject<User | null>(null);

  httpClient.interceptors.request.use(

    async (config: AxiosRequestConfig): Promise<AxiosRequestConfig> => {
      await queue.onIdle();

      if (user.value) {
        config.headers.setAuthorization(`${user.value.tokenType} ${user.value.accessToken}`);
      }
      else {
        config.headers.setAuthorization(null);
      }

      return config;
    },

    (error: AxiosError): Promise<AxiosError> => {
      console.error(`[request error] [${JSON.stringify(error)}]`);
      return Promise.reject(error);
    });

  httpClient.interceptors.response.use(

    (response: AxiosResponse): AxiosResponse => response,

    async (error: AxiosError | Error): Promise<AxiosError | Error> => {

      if (isUnauthorizedError(error) && user.value) {

        let resolves: { (value: unknown): void; }[] = [];

        try {

          queue.add(() => new Promise((_resolve, _reject) => { resolves.push(_resolve) }));

          const response = await options.tokens.refresh(axios.create(options), user.value.refreshToken);
          user.next(response.data);
        }
        catch (refreshError: any) {

          if (isBadRequestError(refreshError)) {
            user.next(null);
          }
        }
        finally {
          resolves.forEach(resolve => resolve(null));
        }
      }

      throw error;
    });

  axiosRetry(httpClient, {
    retries: 3,
    retryCondition: (error) => axiosRetry.isNetworkOrIdempotentRequestError(error) || isUnauthorizedError(error),
    onRetry: (retryCount, error, config) => {
      if (isUnauthorizedError(error) && !user.value)
        throw error;
    }
  });

  const signIn = (credentials: AuthCredentials) =>
    options.tokens.generate(axios.create(options), null, credentials)
      .then(response => {
        user.next(response.data);
        return response;
      });

  const signInWith = (provider: string) =>
    options.tokens.generate(axios.create(options), provider, null)
      .then(response => {
        user.next(response.data);
        return response;
      });

  const signOut = () => options.tokens.revoke(axios.create(options), user.value?.refreshToken || '')
    .finally(() => user.next(null));

  return { ...httpClient, signIn, signInWith, signOut, user } as HttpClient;
};

export interface ExternalWindowFeatures {
    width?: number;
    height?: number;
    left?: number;
    top?: number;
    menubar?: boolean;
    toolbar?: boolean;
    location?: boolean;
    resizable?: boolean;
    scrollbars?: boolean;
    status?: boolean;
    center?: boolean;
}

export class ExternalWindow {
    private name: string;
    private uri: string;
    private features: ExternalWindowFeatures;
    private watcherDelay: number;
    private windowRef: Window | null;
    private watcherId: number | null;
    private watcherRunning: boolean;
    private windowOpen: boolean;
    private resolve: (value?: any) => void;
    private reject: (reason?: any) => void;

    constructor(uri: string, features: ExternalWindowFeatures) {
        this.name = 'external-window-' + new Date().getTime() + "-" + Math.floor(10e12 * Math.random());
        this.uri = uri;
        this.features = features;
        this.watcherDelay = 100;
        this.windowRef = null;
        this.watcherId = null;
        this.watcherRunning = false;
        this.windowOpen = false;
        this.resolve = () => { };
        this.reject = () => { };
        this.onPostMessage = this.onPostMessage.bind(this);
    }

    private getFeaturesString(features: ExternalWindowFeatures): string {
        const defaultFeatures: ExternalWindowFeatures = {
            width: 600,
            height: 600,
            menubar: 'no',
            toolbar: 'no',
            location: 'no',
            resizable: 'yes',
            scrollbars: 'yes',
            status: 'no',
        };
        const mergedFeatures = { ...defaultFeatures, ...features };

        if (mergedFeatures.center) {
            const screenLeft = window.screenLeft || window.screenX || 0;
            const screenTop = window.screenTop || window.screenY || 0;
            const screenWidth = window.innerWidth || document.documentElement.clientWidth || screen.width;
            const screenHeight = window.innerHeight || document.documentElement.clientHeight || screen.height;
            const left = Math.round(screenLeft + (screenWidth - (mergedFeatures.width || 0)) / 2);
            const top = Math.round(screenTop + (screenHeight - (mergedFeatures.height || 0)) / 2);
            mergedFeatures.left = left;
            mergedFeatures.top = top;
        }

        const getValue = (value: boolean): string => {
            if (value === true || value === false) {
                return value ? 'yes' : 'no';
            }
            return '' + value;
        };

        const featureStrings: string[] = [];
        for (const key in mergedFeatures) {
            if (mergedFeatures.hasOwnProperty(key)) {
                const val = getValue((<any>mergedFeatures)[key]);
                if (val !== undefined) {
                    featureStrings.push(`${key}=${val}`);
                }
            }
        }
        return featureStrings.join(',');

    }

    private createPromise(): Promise<any> {
        const module: any = {};

        module.promise = new Promise((resolve, reject) => {
            module.resolve = resolve;
            module.reject = reject;
        });

        this.resolve = module.resolve;
        this.reject = module.reject;

        return module.promise;
    }

    private isAlive(): boolean {
        return !!this.windowRef && !this.windowRef.closed;
    }

    private startWatcher(): void {
        if (this.watcherRunning) {
            throw new Error('Watcher is already started');
        }

        this.watcherId = window.setInterval(() => {
            if (this.watcherRunning && !this.isAlive()) {
                this.close();
            }
        }, this.watcherDelay);

        this.watcherRunning = true;
    }

    private stopWatcher(): void {
        if (!this.watcherRunning) {
            throw new Error('Watcher is already stopped');
        }

        this.watcherRunning = false;
        window.clearInterval(this.watcherId!);
    }

    private onPostMessage(event: MessageEvent<unknown>) {
        const originRegexp = new RegExp('^[^:/?]+://[^/]*');
        const expectedOriginMatches = originRegexp.exec(this.uri);
        const expectedOrigin =
            (expectedOriginMatches && expectedOriginMatches[0]) || location.origin;
        if (this.windowRef === event.source && event.origin === expectedOrigin) {
            if (typeof event.data === 'object' && event.data !== null && 'error' in event.data) {
                this.reject(event.data.error);
            } else {
                this.resolve(event.data);
            }
            this.close();
        }
    }

    public open(): Promise<any> {
        if (this.windowOpen) {
            throw new Error('Window is already open');
        }

        this.windowOpen = true;
        const promise = this.createPromise();
        this.windowRef = window.open(this.uri, '_blank', this.getFeaturesString(this.features));

        if (!this.windowRef) {
            this.reject('blocked');
        } else {
            window.addEventListener('message', this.onPostMessage, true);
            this.startWatcher();
        }

        return promise;
    }

    public close(): void {
        if (!this.windowOpen) {
            throw new Error('Window is already closed');
        }

        this.stopWatcher();
        window.removeEventListener('message', this.onPostMessage);

        if (this.isAlive()) {
            this.windowRef!.close();
        }

        this.reject('closed');
        this.windowRef = null;
        this.windowOpen = false;
    }
}

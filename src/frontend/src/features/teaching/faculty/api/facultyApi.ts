/**
 * Re-exports Teaching faculty Axios API client functions and object.
 */
import { teachingApiClient } from './teachingApiClient';

export * from './teachingApiClient';
export const facultyApi = teachingApiClient;
export default teachingApiClient;
